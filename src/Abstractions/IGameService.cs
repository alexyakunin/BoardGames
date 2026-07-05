using MessagePack;

namespace BoardGames.Abstractions;

public interface IGameService : IComputeService
{
    // Commands
    [CommandHandler]
    Task<Game> Create(Game_Create command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Join(Game_Join command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Start(Game_Start command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Move(Game_Move command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Edit(Game_Edit command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod(MinCacheDuration = 1)]
    Task<Game?> TryGet(string id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 1)]
    Task<ImmutableList<Game>> ListOwn(string? engineId, GameStage? stage, int count, Session session, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 1)]
    Task<ImmutableList<Game>> List(string? engineId, GameStage? stage, int count, CancellationToken cancellationToken = default);
}

// Command markers

public interface IGameCommand : ISessionCommand { }
public interface IGameCommand<TResult> : ISessionCommand<TResult>, IGameCommand { }

// Commands

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Game_Create(
    Session Session,
    string EngineId
) : IGameCommand<Game>;

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Game_Join(
    Session Session,
    string Id,
    bool Join = true
) : IGameCommand<Unit>;

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Game_Start(
    Session Session,
    string Id
) : IGameCommand<Unit>;

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Game_Move(
    Session Session,
    string Id,
    GameMove Move
) : IGameCommand<Unit>;

[MessagePackObject(true)]
// ReSharper disable once InconsistentNaming
public sealed partial record Game_Edit(
    Session Session,
    string Id,
    bool? IsPublic = null,
    int? RoundCount = null,
    string? Intro = null
) : IGameCommand<Unit>;
