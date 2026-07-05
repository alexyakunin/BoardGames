using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;

namespace BoardGames.HostServices;

public class GameService : DbServiceBase<AppDbContext>, IGameService
{
    private readonly Lazy<IMessageParser> _messageParserLazy;
    protected ImmutableDictionary<string, IGameEngine> GameEngines { get; }
    protected IAuth Auth { get; }
    protected IDbEntityResolver<string, DbGame> GameResolver { get; }
    protected IMessageParser MessageParser => _messageParserLazy.Value;

    public GameService(IServiceProvider services) : base(services)
    {
        GameEngines = services.GetRequiredService<ImmutableDictionary<string, IGameEngine>>();
        Auth = services.GetRequiredService<IAuth>();
        GameResolver = services.DbEntityResolver<string, DbGame>();
        _messageParserLazy = new Lazy<IMessageParser>(services.GetRequiredService<IMessageParser>);
    }

    // Commands

    public virtual async Task<Game> Create(Game_Create command, CancellationToken cancellationToken = default)
    {
        var (session, engineId) = command;
        var engine = GameEngines[engineId]; // Just to check it exists
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            InvalidateGameRelatedComputed(context);
            return null!;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);

        var game = new Game() {
            Id = Ulid.NewUlid().ToString(),
            EngineId = engineId,
            UserId = userId,
            Intro = "",
            CreatedAt = Clocks.SystemClock.Now,
            Stage = GameStage.New,
            Players = ImmutableList<GamePlayer>.Empty.Add(new GamePlayer(userId))
        };
        game = engine.Create(game);
        var dbGame = new DbGame();
        dbGame.UpdateFrom(game);
        dbContext.Add(dbGame);
        await dbContext.SaveChangesAsync(cancellationToken);
        context.Operation.Items.KeylessSet(game);
        return game;
    }

    public virtual async Task Join(Game_Join command, CancellationToken cancellationToken = default)
    {
        var (session, id, join) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            InvalidateGameRelatedComputed(context);
            return;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbGame = await GetDbGame(dbContext, id, cancellationToken);
        var game = dbGame.ToModel();
        var engine = GameEngines[game.EngineId];

        if (game.Stage != GameStage.New)
            throw new InvalidOperationException("Game has already been started.");
        if (join) {
            if (game.Players.Any(p => p.UserId == userId))
                throw new InvalidOperationException("You've already joined this game.");
            if (game.Players.Count > engine.MaxPlayerCount)
                throw new InvalidOperationException("You can't join this game: there too many players already.");
            game = game with { Players = game.Players.Add(new GamePlayer(userId)) };
        } else { // Leave
            var leftPlayer = game.Players.SingleOrDefault(p => p.UserId == userId);
            if (leftPlayer == null)
                throw new InvalidOperationException("You've already left this game.");
            game = game with { Players = game.Players.Remove(leftPlayer) };
            context.Operation.Items.KeylessSet(leftPlayer);
        }

        dbGame.UpdateFrom(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        context.Operation.Items.KeylessSet(game);

        // Try auto-start
        if (join && engine.AutoStart && game.Players.Count == engine.MaxPlayerCount) {
            await Commander.Call(new Game_Start(session, id), cancellationToken);
        }
    }

    public virtual async Task Start(Game_Start command, CancellationToken cancellationToken = default)
    {
        var (session, id) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            InvalidateGameRelatedComputed(context);
            return;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbGame = await GetDbGame(dbContext, id, cancellationToken);
        var game = dbGame.ToModel();
        var engine = GameEngines[game.EngineId];

        if (game.Stage != GameStage.New)
            throw new InvalidOperationException("Game has already been started.");
        if (game.UserId != userId && !engine.AutoStart)
            throw new InvalidOperationException("Only the creator of the game can start it.");
        if (game.Players.Count < engine.MinPlayerCount)
            throw new InvalidOperationException(
                $"{engine.MinPlayerCount - game.Players.Count} more player(s) must join to start the game.");
        if (game.Players.Count > engine.MaxPlayerCount)
            throw new InvalidOperationException(
                $"Too many players: {engine.MaxPlayerCount - game.Players.Count} player(s) must leave to start the game.");

        context.Operation.Items.Set("PrevStage", game.Stage); // Saving prev. stage
        var now = Clocks.SystemClock.Now;
        game = game with {
            StartedAt = now,
            LastMoveAt = now,
            Stage = GameStage.Playing,
        };
        game = engine.Start(game);
        dbGame.UpdateFrom(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        context.Operation.Items.KeylessSet(game);
    }

    public virtual async Task Move(Game_Move command, CancellationToken cancellationToken = default)
    {
        var (session, id, move) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            InvalidateGameRelatedComputed(context);
            return;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbGame = await GetDbGame(dbContext, id, cancellationToken);
        var game = dbGame.ToModel();
        var engine = GameEngines[game.EngineId];

        if (game.Stage != GameStage.Playing)
            throw new InvalidOperationException("Game has already ended or hasn't started yet.");
        var player = game.Players.SingleOrDefault(p => p.UserId == userId);
        if (player == null)
            throw new InvalidOperationException("You aren't a participant of this game.");
        var playerIndex = game.Players.IndexOf(player);
        var now = Clocks.SystemClock.Now;
        move = move with {
            PlayerIndex = playerIndex,
            Time = now,
        };

        context.Operation.Items.Set("PrevStage", game.Stage); // Saving prev. stage
        game = engine.Move(game, move) with { LastMoveAt = now };
        if (game.Stage == GameStage.Ended)
            game = game with { EndedAt = now };
        dbGame.UpdateFrom(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        context.Operation.Items.KeylessSet(game);
    }

    public virtual async Task Edit(Game_Edit command, CancellationToken cancellationToken = default)
    {
        var session = command.Session;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            InvalidateGameRelatedComputed(context);
            return;
        }

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var parsedIntro = command.Intro == null
            ? null
            : await MessageParser.Parse(command.Intro, cancellationToken);

        var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var dbGame = await GetDbGame(dbContext, command.Id, cancellationToken);
        if (command.IsPublic.HasValue)
            dbGame.IsPublic = command.IsPublic.Value;
        if (command.RoundCount.HasValue) {
            if (!dbGame.RoundCount.HasValue)
                throw new InvalidOperationException("This game doesn't have rounds.");
            var roundCount = command.RoundCount.Value;
            if (roundCount is < 1 or > 100)
                throw new InvalidOperationException("Round count must be an integer in [1..100] range.");
            dbGame.RoundCount = roundCount;
        }
        if (parsedIntro != null)
            dbGame.Intro = parsedIntro.Format();
        var game = dbGame.ToModel();
        await dbContext.SaveChangesAsync(cancellationToken);
        context.Operation.Items.KeylessSet(game);
    }

    // Queries

    public virtual async Task<Game?> TryGet(string id, CancellationToken cancellationToken = default)
    {
        var dbGame = await GameResolver.Get(id, cancellationToken);
        return dbGame?.ToModel();
    }

    public virtual async Task<ImmutableList<Game>> ListOwn(
        string? engineId, GameStage? stage, int count, Session session,
        CancellationToken cancellationToken = default)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count));

        var user = await Auth.GetUser(session, cancellationToken);
        user = user.Require(User.MustBeAuthenticated);
        var userId = long.Parse(user.Id);
        await PseudoListOwn(user.Id, cancellationToken);

        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var games = dbContext.Games.AsQueryable().Where(g => g.Players.Any(p => p.DbUserId == userId));
        if (engineId != null)
            games = games.Where(g => g.EngineId == engineId);
        games = ApplyStageFilter(games, stage);
        var gameIds = await games.Select(g => g.Id).Take(count)
            .ToListAsync(cancellationToken);
        return await GetMany(gameIds, cancellationToken);
    }

    public virtual async Task<ImmutableList<Game>> List(
        string? engineId, GameStage? stage, int count,
        CancellationToken cancellationToken = default)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count));

        await PseudoList(engineId, stage, cancellationToken);

        var dbContext = await DbHub.CreateDbContext(cancellationToken);
        await using var _1 = dbContext.ConfigureAwait(false);
        var games = dbContext.Games.AsQueryable().Where(g => g.IsPublic);
        if (engineId != null)
            games = games.Where(g => g.EngineId == engineId);
        games = ApplyStageFilter(games, stage);
        var gameIds = await games.Select(g => g.Id).Take(count)
            .ToListAsync(cancellationToken);
        return await GetMany(gameIds, cancellationToken);
    }

    // Invalidation

    // Common invalidation logic for all game commands.
    // It's called from the Invalidation.IsActive blocks of all command handlers here.
    protected void InvalidateGameRelatedComputed(CommandContext context)
    {
        var operationItems = context.Operation.Items;
        var game = operationItems.KeylessGet<Game>();
        if (game == null)
            return;
        var prevStage = operationItems.Get("PrevStage", game.Stage);

        // The game itself
        _ = TryGet(game.Id, default);

        // Own games of all affected players
        foreach (var gamePlayer in game.Players)
            _ = PseudoListOwn(gamePlayer.UserId.ToString(), default);
        var leftPlayer = operationItems.KeylessGet<GamePlayer>();
        if (leftPlayer != null)
            _ = PseudoListOwn(leftPlayer.UserId.ToString(), default);

        // Global lists
        _ = PseudoList(game.EngineId, game.Stage, default);
        _ = PseudoList(game.EngineId, null, default);
        _ = PseudoList(null, game.Stage, default);
        _ = PseudoList(null, null, default);
        if (prevStage != game.Stage) {
            _ = PseudoList(game.EngineId, prevStage, default);
            _ = PseudoList(null, prevStage, default);
        }
    }

    [ComputeMethod]
    protected virtual Task<Unit> PseudoListOwn(string userId, CancellationToken cancellationToken = default)
        => TaskExt.UnitTask;
    [ComputeMethod]
    protected virtual Task<Unit> PseudoList(string? engineId, GameStage? stage, CancellationToken cancellationToken = default)
        => TaskExt.UnitTask;

    // Protected methods

    protected static IQueryable<DbGame> ApplyStageFilter(IQueryable<DbGame> games, GameStage? stage)
    {
        if (stage == null)
            return games.OrderByDescending(g => g.CreatedAt);
        games = games.Where(g => g.Stage == stage.GetValueOrDefault());
        return stage.GetValueOrDefault() switch {
            GameStage.New => games.OrderByDescending(g => g.CreatedAt),
            GameStage.Playing => games.OrderByDescending(g => g.StartedAt),
            GameStage.Ended => games.OrderByDescending(g => g.EndedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(stage)),
        };
    }

    protected async Task<ImmutableList<Game>> GetMany(IEnumerable<string> gameIds, CancellationToken cancellationToken)
    {
        var result = await gameIds.ParallelSelectToList(TryGet, cancellationToken);
        return ImmutableList<Game>.Empty.AddRange(result.Where(g => g != null)!);
    }

    protected async Task<DbGame> GetDbGame(AppDbContext dbContext, string id, CancellationToken cancellationToken)
    {
        var dbGame = await FindDbGame(dbContext, id, cancellationToken);
        if (dbGame == null)
            throw new KeyNotFoundException("Game not found.");
        return dbGame;
    }

    protected async Task<DbGame?> FindDbGame(AppDbContext dbContext, string id, CancellationToken cancellationToken)
    {
        var dbGame = await dbContext.Games
            .Include(g => g.Players)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken);
        return dbGame;
    }
}
