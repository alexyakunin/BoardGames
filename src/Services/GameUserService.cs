using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGames.Abstractions;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework;

namespace BoardGames.Services
{
    [ComputeService(typeof(IGameUserService))]
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

        [ComputeMethod(KeepAliveTime = 60, AutoInvalidateTime = 61)]
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
            return session.LastSeenAt < minLastSeenAt;
        }
    }
}
