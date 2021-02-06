using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BoardGames.Abstractions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController, JsonifyErrors]
    public class GameUserController : ControllerBase, IGameUserService
    {
        protected IGameUserService GameUsers { get; }
        protected ISessionResolver SessionResolver { get; }

        public GameUserController(IGameUserService gameUsers, ISessionResolver sessionResolver)
        {
            GameUsers = gameUsers;
            SessionResolver = sessionResolver;
        }

        // Queries

        [HttpGet("find/{id}"), Publish]
        public Task<GameUser?> FindAsync([FromRoute] long id, CancellationToken cancellationToken = default)
            => GameUsers.FindAsync(id, cancellationToken);

        [HttpGet("isOnline/{id}"), Publish]
        public Task<bool> IsOnlineAsync([FromRoute] long id, CancellationToken cancellationToken = default)
            => GameUsers.IsOnlineAsync(id, cancellationToken);
    }
}
