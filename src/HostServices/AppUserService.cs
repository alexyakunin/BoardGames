using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.Operations;
using Stl.Text;

namespace BoardGames.HostServices
{
    [ComputeService, ServiceAlias(typeof(IAppUserService))]
    public class AppUserService : DbServiceBase<AppDbContext>, IAppUserService
    {
        protected IServerSideAuthService AuthService { get; }
        protected IUserNameService UserNameService { get; }
        protected IDbUserRepo<AppDbContext> DbUsers { get; }

        public AppUserService(IServiceProvider services) : base(services)
        {
            AuthService = services.GetRequiredService<IServerSideAuthService>();
            UserNameService = services.GetRequiredService<IUserNameService>();
            DbUsers = services.GetRequiredService<IDbUserRepo<AppDbContext>>();
        }

        public virtual async Task<AppUser?> FindAsync(long id, CancellationToken cancellationToken = default)
        {
            var user = await AuthService.TryGetUser(id.ToString(), cancellationToken);
            return user == null ? null : new AppUser() { Id = id, Name = user.Name };
        }

        public virtual async Task<AppUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            var user = await dbContext.Users.AsQueryable()
                .Where(u => u.Name == name)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return user == null ? null : new AppUser() { Id = user.Id, Name = user.Name };
        }

        [ComputeMethod(AutoInvalidateTime = 61)]
        public virtual async Task<bool> IsOnlineAsync(long id, CancellationToken cancellationToken = default)
        {
            await using var dbContext = CreateDbContext();
            var session = dbContext.Sessions.AsQueryable()
                .Where(s => s.UserId == id)
                .OrderByDescending(s => s.LastSeenAt)
                .FirstOrDefault();
            if (session == null)
                return false;
            var minLastSeenAt = Clock.UtcNow - TimeSpan.FromMinutes(2);
            return session.LastSeenAt >= minLastSeenAt;
        }

        // Takes care of invalidation of IsOnlineAsync once user signs in
        [CommandHandler(IsFilter = true, Priority = 1)]
        protected virtual async Task OnSignInAsync(SignInCommand command, CancellationToken cancellationToken)
        {
            var context = CommandContext.GetCurrent();
            await context.InvokeRemainingHandlers(cancellationToken);
            if (Computed.IsInvalidating()) {
                var invSessionInfo = context.Operation().Items.TryGet<SessionInfo>();
                if (invSessionInfo != null && long.TryParse(invSessionInfo.UserId, out var invUserId))
                    IsOnlineAsync(invUserId, default).Ignore();
                return;
            }

            // The code below renames the user if it happens that its name isn't unique.
            // And if you check out how CreateCommandDbContext works,
            // you'll find out this code actually shared the same transaction & connection
            // as the original sign-in command handler!
            await using var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
            var sessionInfo = context.Operation().Items.Get<SessionInfo>();
            var userId = long.Parse(sessionInfo.UserId);
            var dbUser = await DbUsers.TryGet(dbContext, userId, cancellationToken);
            var newName = await NormalizeNameAsync(dbContext, dbUser!.Name, userId, cancellationToken);
            if (newName != dbUser.Name) {
                dbUser.Name = newName;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // Validates user name on edit
        [CommandHandler(IsFilter = true, Priority = 1)]
        protected virtual async Task OnEditUserAsync(EditUserCommand command, CancellationToken cancellationToken)
        {
            var (session, name) = command;
            var context = CommandContext.GetCurrent();
            if (Computed.IsInvalidating()) {
                await context.InvokeRemainingHandlers(cancellationToken);
                if (name != null)
                    FindByNameAsync(name, default).Ignore();
                return;
            }
            if (name != null) {
                var error = UserNameService.ValidateName(name);
                if (error != null)
                    throw error;

                var user = await AuthService.GetUser(session, cancellationToken);
                user = user.MustBeAuthenticated();
                var userId = long.Parse(user.Id);

                await using var dbContext = CreateDbContext();
                if (dbContext.Users.Any(u => u.Name == name && u.Id != userId))
                    throw new InvalidOperationException("This name is already used by someone else.");
            }
            await context.InvokeRemainingHandlers(cancellationToken);
        }

        private async Task<string> NormalizeNameAsync(AppDbContext dbContext, string name, long userId, CancellationToken cancellationToken = default)
        {
            // Normalizing name
            var sb = StringBuilderEx.Acquire();
            foreach (var c in name) {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    sb.Append(c);
                else if (sb.Length == 0 || char.IsLetterOrDigit(sb[^1]))
                    sb.Append('_');
            }
            name = sb.ToStringAndRelease();
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
}
