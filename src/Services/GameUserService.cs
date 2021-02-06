using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGames.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Operations;

namespace BoardGames.Services
{
    [ComputeService, ServiceAlias(typeof(IGameUserService))]
    public class GameUserService : DbServiceBase<AppDbContext>, IGameUserService
    {
        protected IServerSideAuthService AuthService { get; }

        public GameUserService(IServiceProvider services, IServerSideAuthService authService) : base(services)
            => AuthService = authService;

        public virtual async Task<GameUser?> FindAsync(long id, CancellationToken cancellationToken = default)
        {
            var user = await AuthService.TryGetUserAsync(id.ToString(), cancellationToken);
            return user == null ? null : new GameUser() { Id = id, Name = user.Name };
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
            await context.InvokeRemainingHandlersAsync(cancellationToken);
            if (Computed.IsInvalidating()) {
                var sessionInfo = context.Operation().Items.TryGet<SessionInfo>();
                if (sessionInfo != null && long.TryParse(sessionInfo.UserId, out var userId))
                    IsOnlineAsync(userId, default).Ignore();
            }
        }
    }
}
