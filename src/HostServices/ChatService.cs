using System.Security;
using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;

namespace BoardGames.HostServices;

public class ChatService : DbServiceBase<AppDbContext>, IChatService
{
    private readonly Lazy<IMessageParser> _messageParserLazy;
    protected IAuth Auth { get; }
    protected IAppUserService AppUsers { get; }
    protected IGameService Games { get; }
    protected IMessageParser MessageParser => _messageParserLazy.Value;

    public ChatService(IServiceProvider services) : base(services)
    {
        Auth = services.GetRequiredService<IAuth>();
        AppUsers = services.GetRequiredService<IAppUserService>();
        Games = services.GetRequiredService<IGameService>();
        _messageParserLazy = new Lazy<IMessageParser>(services.GetRequiredService<IMessageParser>);
    }

    // Commands

    public virtual async Task<ChatMessage> Post(
        Chat_Post command, CancellationToken cancellationToken = default)
    {
        var (session, chatId, text) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            _ = PseudoGetTail(chatId, default);
            return null!;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);
        var cp = await GetPermissions(session, chatId, cancellationToken);
        if ((cp & ChatPermission.Write) != ChatPermission.Write)
            throw new SecurityException("You can't post to this chat.");
        var parsedMessage = await MessageParser.Parse(text, cancellationToken);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        DateTime now = Clocks.SystemClock.Now;
        var chatMessage = new ChatMessage(Ulid.NewUlid().ToString(), chatId) {
            UserId = userId,
            CreatedAt = now,
            EditedAt = now,
            Text = parsedMessage.Format(),
        };
        var dbChatMessage = new DbChatMessage();
        dbChatMessage.UpdateFrom(chatMessage);
        dbContext.Add(dbChatMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        context.Operation.Items.KeylessSet(chatMessage);
        return chatMessage;
    }

    public virtual Task Delete(
        Chat_Delete command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    // Queries

    public virtual async Task<Chat?> TryGet(
        string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(chatId))
            return null;
        if (chatId.StartsWith("game/")) {
            var gameId = chatId[5..];
            var game = await Games.TryGet(gameId, cancellationToken);
            if (game == null)
                return null;
            return new Chat(chatId, ChatKind.Game) {
                OwnerIds = ImmutableHashSet<long>.Empty.Add(game.UserId),
                ParticipantIds = game.Players.Select(p => p.UserId).ToImmutableHashSet(),
                IsPublic = game.IsPublic,
            };
        }
        if (chatId.StartsWith("p2p/")) {
            var dividerIndex = chatId.IndexOf('/', 4);
            if (dividerIndex < 0)
                return null;
            if (!long.TryParse(chatId[4..dividerIndex], out var user1Id))
                return null;
            if (!long.TryParse(chatId[(dividerIndex + 1)..], out var user2Id))
                return null;
            if (user1Id >= user2Id)
                return null;
            var user1 = await AppUsers.TryGet(user1Id, cancellationToken);
            if (user1 == null)
                return null;
            var user2 = await AppUsers.TryGet(user2Id, cancellationToken);
            if (user2 == null)
                return null;
            return new Chat(chatId, ChatKind.P2P) {
                OwnerIds = ImmutableHashSet<long>.Empty,
                ParticipantIds = ImmutableHashSet<long>.Empty.Add(user1Id).Add(user2Id),
                IsPublic = false,
            };
        }
        return null;
    }

    public virtual async Task<ChatPermission> GetPermissions(
        Session session, string chatId, CancellationToken cancellationToken = default)
    {
        var user = await Auth.GetUser(session, cancellationToken);
        if (user == null || !user.IsAuthenticated())
            return 0;
        var chat = await TryGet(chatId, cancellationToken);
        if (chat == null || chat.Kind == ChatKind.Unknown)
            return 0;

        var userId = long.Parse(user.Id);
        if (chat.OwnerIds.Contains(userId))
            return ChatPermission.Owner;
        if (chat.ParticipantIds.Contains(userId))
            return ChatPermission.Write;
        if (chat.Kind == ChatKind.Game)
            return ChatPermission.Write;
        return 0;
    }

    public virtual async Task<ChatPage> GetTail(Session session, string chatId, int limit, CancellationToken cancellationToken = default)
    {
        var cp = await GetPermissions(session, chatId, cancellationToken);
        if ((cp & ChatPermission.Read) != ChatPermission.Read)
            throw new SecurityException("You can't access this chat.");
        return await GetChatTail(chatId, limit, cancellationToken);
    }

    public virtual async Task<long> GetMessageCount(
        string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default)
    {
        await PseudoGetTail(chatId, default);
        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbMessages = dbContext.ChatMessages.AsQueryable();
        if (period.HasValue) {
            var minCreatedAt = (Clocks.SystemClock.Now - period.Value).ToDateTime();
            dbMessages = dbMessages.Where(m => m.DbChatId == chatId && m.CreatedAt >= minCreatedAt);
        } else {
            dbMessages = dbMessages.Where(m => m.DbChatId == chatId);
        }
        return await dbMessages.LongCountAsync(cancellationToken);
    }

    // Protected methods

    [ComputeMethod]
    protected virtual async Task<ChatPage> GetChatTail(string chatId, int limit, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));
        await PseudoGetTail(chatId, default);
        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);

        // Fetching messages
        var dbMessages = await dbContext.ChatMessages.AsQueryable()
            .Where(m => m.DbChatId == chatId)
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var messages = dbMessages.Select(m => m.ToModel()).ToImmutableList();

        // Fetching users in parallel
        var users = new Dictionary<long, AppUser>();
        foreach (var pack in messages.Chunk(128)) {
            var packUsers = await Task.WhenAll(
                pack.Where(m => !users.ContainsKey(m.UserId))
                    .Select(m => AppUsers.TryGet(m.UserId, cancellationToken)));
            foreach (var user in packUsers)
                if (user != null)
                    users.TryAdd(user.Id, user);
        }

        return new ChatPage(chatId, limit) {
            Messages = messages,
            Users = users.ToImmutableDictionary(),
        };
    }

    // Invalidation-related

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGetTail(string chatId, CancellationToken cancellationToken)
        => TaskExt.UnitTask;
}
