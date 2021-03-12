using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion;
using Stl.Fusion.Authentication;

namespace BoardGames.Abstractions
{
    public interface IGameService
    {
        // Commands
        [CommandHandler]
        Task<Game> Create(Game.CreateCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Join(Game.JoinCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Start(Game.StartCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Move(Game.MoveCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Edit(Game.EditCommand command, CancellationToken cancellationToken = default);

        // Queries
        [ComputeMethod(KeepAliveTime = 1)]
        Task<Game?> TryGet(string id, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ImmutableList<Game>> ListOwn(string? engineId, GameStage? stage, int count, Session session, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ImmutableList<Game>> List(string? engineId, GameStage? stage, int count, CancellationToken cancellationToken = default);
    }
}
