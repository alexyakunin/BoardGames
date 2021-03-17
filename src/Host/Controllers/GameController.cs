using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;
using Stl.Fusion.Authentication;
using BoardGames.Abstractions;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class GameController : ControllerBase, IGameService
    {
        protected IGameService Games { get; }
        protected ISessionResolver SessionResolver { get; }

        public GameController(IGameService games, ISessionResolver sessionResolver)
        {
            Games = games;
            SessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost]
        public Task<Game> Create([FromBody] Game.CreateCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Games.Create(command, cancellationToken);
        }

        [HttpPost]
        public Task Join([FromBody] Game.JoinCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Games.Join(command, cancellationToken);
        }

        [HttpPost]
        public Task Start([FromBody] Game.StartCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Games.Start(command, cancellationToken);
        }

        [HttpPost]
        public Task Move([FromBody] Game.MoveCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Games.Move(command, cancellationToken);
        }

        [HttpPost]
        public Task Edit([FromBody] Game.EditCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Games.Edit(command, cancellationToken);
        }

        // Queries

        [HttpGet("{id}"), Publish]
        public Task<Game?> TryGet([FromRoute] string id, CancellationToken cancellationToken = default)
        {
            id = HttpUtility.UrlDecode(id);
            return Games.TryGet(id, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<ImmutableList<Game>> ListOwn(
            string? engineId, GameStage? stage, int count, Session? session,
            CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Games.ListOwn(engineId, stage, count, session, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<ImmutableList<Game>> List(
            string? engineId, GameStage? stage, int count,
            CancellationToken cancellationToken = default)
            => Games.List(engineId, stage, count, cancellationToken);
    }
}
