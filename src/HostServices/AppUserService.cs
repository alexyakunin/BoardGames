using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;

namespace BoardGames.HostServices;

public class AppUserService : DbServiceBase<AppDbContext>, IAppUserService
{
    protected IAuth Auth { get; }
    protected IAuthBackend AuthBackend { get; }
    protected IUserNameService UserNameService { get; }

    public AppUserService(IServiceProvider services) : base(services)
    {
        Auth = services.GetRequiredService<IAuth>();
        AuthBackend = services.GetRequiredService<IAuthBackend>();
        UserNameService = services.GetRequiredService<IUserNameService>();
    }

    public virtual async Task<AppUser?> TryGet(long id, CancellationToken cancellationToken = default)
    {
        var user = await AuthBackend.GetUser(DbShard.Single, id.ToString(), cancellationToken);
        return user == null ? null : new AppUser(id, user.Name);
    }

    public virtual async Task<AppUser?> TryGetByName(string name, CancellationToken cancellationToken = default)
    {
        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var user = await dbContext.Users.AsQueryable()
            .Where(u => u.Name == name)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return user == null ? null : new AppUser(user.Id, user.Name);
    }

    [ComputeMethod(AutoInvalidationDelay = 61)]
    public virtual async Task<bool> IsOnline(long id, CancellationToken cancellationToken = default)
    {
        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var minLastSeenAt = (Clocks.SystemClock.Now - TimeSpan.FromMinutes(2)).ToDateTime();
        var session = await dbContext.Sessions.AsQueryable()
            .Where(s => s.UserId == id)
            .OrderByDescending(s => s.LastSeenAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (session == null)
            return false;
        return session.LastSeenAt >= minLastSeenAt;
    }

    // Invalidates IsOnline & TryGetByName on auth operation completions.
    // Note that in the current Fusion the invalidation pass invokes just the final
    // command handler, so filter handlers can't handle invalidation anymore -
    // an ICompletion handler like this one is the way to react to "foreign" commands.
    [CommandHandler(IsFilter = true, Priority = 100)]
    protected virtual async Task OnAuthCompletion(ICompletion completion, CancellationToken cancellationToken)
    {
        var context = CommandContext.GetCurrent();
        await context.InvokeRemainingHandlers(cancellationToken);
        var operation = completion.Operation;
        switch (operation.Command) {
        case AuthBackend_SignIn:
            var sessionInfo = operation.Items.KeylessGet<SessionInfo>();
            if (sessionInfo != null && long.TryParse(sessionInfo.UserId, out var signedInUserId)) {
                using (Invalidation.Begin())
                    _ = IsOnline(signedInUserId, default);
            }
            break;
        case Auth_EditUser { Name: { } newName }:
            using (Invalidation.Begin())
                _ = TryGetByName(newName, default);
            break;
        }
    }

    // Renames the user on sign-in if it happens that its name isn't unique
    [CommandHandler(IsFilter = true, Priority = 1)]
    protected virtual async Task OnSignIn(AuthBackend_SignIn command, CancellationToken cancellationToken)
    {
        var context = CommandContext.GetCurrent();
        await context.InvokeRemainingHandlers(cancellationToken);
        if (Invalidation.IsActive)
            return;

        // The code below renames the user if it happens that its name isn't unique.
        // Note that it shares the same transaction & connection as the original
        // sign-in command handler.
        var sessionInfo = context.Operation.Items.KeylessGet<SessionInfo>();
        if (sessionInfo == null || !long.TryParse(sessionInfo.UserId, out var userId))
            return;

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbUser = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (dbUser == null)
            return;
        var newName = await NormalizeName(dbContext, dbUser.Name, userId, cancellationToken);
        if (newName != dbUser.Name) {
            dbUser.Name = newName;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    // Validates user name on edit
    [CommandHandler(IsFilter = true, Priority = 1)]
    protected virtual async Task OnEditUser(Auth_EditUser command, CancellationToken cancellationToken)
    {
        var (session, name) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            await context.InvokeRemainingHandlers(cancellationToken);
            return;
        }
        if (name != null) {
            var error = UserNameService.ValidateName(name);
            if (error != null)
                throw error;

            var user = await Auth.GetUser(session, cancellationToken);
            user = user.Require(User.MustBeAuthenticated);
            var userId = long.Parse(user.Id);

            var dbContext = await DbHub.CreateDbContext(cancellationToken);
            await using var _1 = dbContext.ConfigureAwait(false);
            if (await dbContext.Users.AnyAsync(u => u.Name == name && u.Id != userId, cancellationToken))
                throw new InvalidOperationException("This name is already used by someone else.");
        }
        await context.InvokeRemainingHandlers(cancellationToken);
    }

    private async Task<string> NormalizeName(AppDbContext dbContext, string name, long userId, CancellationToken cancellationToken = default)
    {
        // Normalizing name
        var sb = new StringBuilder();
        foreach (var c in name) {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                sb.Append(c);
            else if (sb.Length == 0 || char.IsLetterOrDigit(sb[^1]))
                sb.Append('_');
        }
        name = sb.ToString();
        if (name.Length < 4 || !char.IsLetter(name[0]))
            name = "user-" + name;

        // Finding the number @ the tail
        var numberStartIndex = name.Length;
        for (; numberStartIndex >= 1; numberStartIndex--) {
            if (!char.IsNumber(name[numberStartIndex - 1]))
                break;
        }

        // Iterating through these tail numbers to get the unique user name
        var namePrefix = name.Substring(0, numberStartIndex);
        var nameSuffix = name.Substring(numberStartIndex);
        var nextNumber = long.TryParse(nameSuffix, out var number) ? number + 1 : 1;
        while (true) {
            var isNameUsed = await dbContext.Users.AsQueryable()
                .AnyAsync(u => u.Name == name && u.Id != userId, cancellationToken);
            if (!isNameUsed)
                break;
            name = namePrefix + nextNumber++;
        }
        return name;
    }
}
