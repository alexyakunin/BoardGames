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
    public class AppUserController : ControllerBase, IAppUserService
    {
        protected IAppUserService AppUsers { get; }
        protected ISessionResolver SessionResolver { get; }

        public AppUserController(IAppUserService appUsers, ISessionResolver sessionResolver)
        {
            AppUsers = appUsers;
            SessionResolver = sessionResolver;
        }

        // Queries

        [HttpGet("find/{id}"), Publish]
        public Task<AppUser?> FindAsync([FromRoute] long id, CancellationToken cancellationToken = default)
            => AppUsers.FindAsync(id, cancellationToken);

        [HttpGet("findByName/{name}"), Publish]
        public Task<AppUser?> FindByNameAsync([FromRoute] string name, CancellationToken cancellationToken = default)
            => AppUsers.FindByNameAsync(name, cancellationToken);

        [HttpGet("isOnline/{id}"), Publish]
        public Task<bool> IsOnlineAsync([FromRoute] long id, CancellationToken cancellationToken = default)
            => AppUsers.IsOnlineAsync(id, cancellationToken);
    }
}
