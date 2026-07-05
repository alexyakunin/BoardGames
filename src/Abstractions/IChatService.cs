using MessagePack;

namespace BoardGames.Abstractions;

public interface IChatService : IComputeService
{
    // Commands
    [CommandHandler]
    Task<ChatMessage> Post(Chat_Post command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Delete(Chat_Delete command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 1)]
    Task<Chat?> TryGet(string chatId, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 1)]
    Task<ChatPermission> GetPermissions(Session session, string chatId, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 1)]
    Task<ChatPage> GetTail(Session session, string chatId, int limit, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 1)]
    Task<long> GetMessageCount(string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default);
}

// Command markers

public interface IChatCommand : ISessionCommand
{
    string ChatId { get; }
}
public interface IChatCommand<TResult> : ISessionCommand<TResult>, IChatCommand { }

// Commands

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Chat_Post(
    Session Session,
    string ChatId,
    string Text
) : IChatCommand<ChatMessage>;

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Chat_Delete(
    Session Session,
    string ChatId,
    string MessageId
) : IChatCommand<Unit>;
