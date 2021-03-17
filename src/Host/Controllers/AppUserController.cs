using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using BoardGames.Abstractions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]/[action]")]
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

        [HttpGet("{id}"), Publish]
        public Task<AppUser?> TryGet([FromRoute] long id, CancellationToken cancellationToken = default)
            => AppUsers.TryGet(id, cancellationToken);

        [HttpGet("{name}"), Publish]
        public Task<AppUser?> TryGetByName([FromRoute] string name, CancellationToken cancellationToken = default)
        {
            name = HttpUtility.UrlDecode(name);
            return AppUsers.TryGetByName(name, cancellationToken);
        }

        [HttpGet("{id}"), Publish]
        public Task<bool> IsOnline([FromRoute] long id, CancellationToken cancellationToken = default)
            => AppUsers.IsOnline(id, cancellationToken);
    }
}
