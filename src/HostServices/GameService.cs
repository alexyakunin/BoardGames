using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion;
using BoardGames.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Operations;
using Stl.Internal;

namespace BoardGames.HostServices
{
    [ComputeService, ServiceAlias(typeof(IGameService))]
    public class GameService : DbServiceBase<AppDbContext>, IGameService
    {
        private readonly Lazy<IMessageParser> _messageParserLazy;
        protected ImmutableDictionary<string, IGameEngine> GameEngines { get; }
        protected IAuthService AuthService { get; }
        protected DbEntityResolver<AppDbContext, string, DbGame> GameResolver { get; }
        protected IMessageParser MessageParser => _messageParserLazy.Value;

        public GameService(IServiceProvider services) : base(services)
        {
            GameEngines = services.GetRequiredService<ImmutableDictionary<string, IGameEngine>>();
            AuthService = services.GetRequiredService<IAuthService>();
            GameResolver = services.GetRequiredService<DbEntityResolver<AppDbContext, string, DbGame>>();
            _messageParserLazy = new Lazy<IMessageParser>(services.GetRequiredService<IMessageParser>);
        }

        // Commands

        public virtual async Task<Game> CreateAsync(Game.CreateCommand command, CancellationToken cancellationToken = default)
        {
            var (session, engineId) = command;
            var engine = GameEngines[engineId]; // Just to check it exists
            var context = CommandContext.GetCurrent();

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var userId = long.Parse(user.Id);

            await using var dbContext = await CreateCommandDbContext(cancellationToken);

            var game = new Game() {
                Id = Ulid.NewUlid().ToString(),
                EngineId = engineId,
                UserId = userId,
                Intro = "",
                CreatedAt = Clock.Now,
                Stage = GameStage.New,
                Players = ImmutableList<GamePlayer>.Empty.Add(new GamePlayer(userId))
            };
            game = engine.Create(game);
            var dbGame = new DbGame();
            dbGame.UpdateFrom(game);
            dbContext.Add(dbGame);
            await dbContext.SaveChangesAsync(cancellationToken);
            context.Operation().Items.Set(game);
            return game;
        }

        public virtual async Task JoinAsync(Game.JoinCommand command, CancellationToken cancellationToken = default)
        {
            var (session, id, join) = command;
            var context = CommandContext.GetCurrent();

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var userId = long.Parse(user.Id);

            await using var dbContext = await CreateCommandDbContext(cancellationToken);
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
                context.Operation().Items.Set(leftPlayer);
            }

            dbGame.UpdateFrom(game);
            await dbContext.SaveChangesAsync(cancellationToken);
            context.Operation().Items.Set(game);

            // Try auto-start
            if (join && engine.AutoStart && game.Players.Count == engine.MaxPlayerCount) {
                await StartAsync(new Game.StartCommand(session, id), cancellationToken);
            }
        }

        public virtual async Task StartAsync(Game.StartCommand command, CancellationToken cancellationToken = default)
        {
            var (session, id) = command;
            var context = CommandContext.GetCurrent();

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var userId = long.Parse(user.Id);

            await using var dbContext = await CreateCommandDbContext(cancellationToken);
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

            context.Operation().Items.Set(Box.New(game.Stage)); // Saving prev. stage
            var now = Clock.Now;
            game = game with {
                StartedAt = now,
                LastMoveAt = now,
                Stage = GameStage.Playing,
            };
            game = engine.Start(game);
            dbGame.UpdateFrom(game);
            await dbContext.SaveChangesAsync(cancellationToken);
            context.Operation().Items.Set(game);
        }

        public virtual async Task MoveAsync(Game.MoveCommand command, CancellationToken cancellationToken = default)
        {
            var (session, id, move) = command;
            var context = CommandContext.GetCurrent();

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var userId = long.Parse(user.Id);

            await using var dbContext = await CreateCommandDbContext(cancellationToken);
            var dbGame = await GetDbGame(dbContext, id, cancellationToken);
            var game = dbGame.ToModel();
            var engine = GameEngines[game.EngineId];

            if (game.Stage != GameStage.Playing)
                throw new InvalidOperationException("Game has already ended or hasn't started yet.");
            var player = game.Players.SingleOrDefault(p => p.UserId == userId);
            if (player == null)
                throw new InvalidOperationException("You aren't a participant of this game.");
            var playerIndex = game.Players.IndexOf(player);
            var now = Clock.Now;
            move = move with {
                PlayerIndex = playerIndex,
                Time = now,
            };

            context.Operation().Items.Set(Box.New(game.Stage)); // Saving prev. stage
            game = engine.Move(game, move) with { LastMoveAt = now };
            if (game.Stage == GameStage.Ended)
                game = game with { EndedAt = now };
            dbGame.UpdateFrom(game);
            await dbContext.SaveChangesAsync(cancellationToken);
            context.Operation().Items.Set(game);
        }

        public virtual async Task EditAsync(Game.EditCommand command, CancellationToken cancellationToken = default)
        {
            var session = command.Session;
            var context = CommandContext.GetCurrent();

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var parsedIntro = command.Intro == null
                ? null
                : await MessageParser.ParseAsync(command.Intro, cancellationToken);

            await using var dbContext = await CreateCommandDbContext(cancellationToken);
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
            context.Operation().Items.Set(game);
        }

        // Queries

        public virtual async Task<Game?> FindAsync(string id, CancellationToken cancellationToken = default)
        {
            var dbGame = await GameResolver.TryGet(id, cancellationToken);
            return dbGame?.ToModel();
        }

        public virtual async Task<ImmutableList<Game>> ListOwnAsync(
            string? engineId, GameStage? stage, int count, Session session,
            CancellationToken cancellationToken = default)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var user = await AuthService.GetUser(session, cancellationToken);
            user = user.MustBeAuthenticated();
            var userId = long.Parse(user.Id);
            await PseudoListOwnAsync(user.Id, cancellationToken);

            await using var dbContext = CreateDbContext();
            var games = dbContext.Games.AsQueryable().Where(g => g.Players.Any(p => p.DbUserId == userId));
            if (engineId != null)
                games = games.Where(g => g.EngineId == engineId);
            if (stage != null) {
                games = games.Where(g => g.Stage == stage.GetValueOrDefault());
                switch (stage.GetValueOrDefault()) {
                case GameStage.New:
                    games = games.OrderByDescending(g => g.CreatedAt);
                    break;
                case GameStage.Playing:
                    games = games.OrderByDescending(g => g.StartedAt);
                    break;
                case GameStage.Ended:
                    games = games.OrderByDescending(g => g.EndedAt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            else {
                games = games.OrderByDescending(g => g.CreatedAt);
            }
            var gameIds = await games.Select(g => g.Id).Take(count)
                .ToListAsync(cancellationToken);
            return await GetManyAsync(gameIds, cancellationToken);
        }

        public virtual async Task<ImmutableList<Game>> ListAsync(
            string? engineId, GameStage? stage, int count,
            CancellationToken cancellationToken = default)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            await PseudoListAsync(engineId, stage, cancellationToken);

            await using var dbContext = CreateDbContext();
            var games = dbContext.Games.AsQueryable().Where(g => g.IsPublic);
            if (engineId != null)
                games = games.Where(g => g.EngineId == engineId);
            if (stage != null) {
                games = games.Where(g => g.Stage == stage.GetValueOrDefault());
                switch (stage.GetValueOrDefault()) {
                case GameStage.New:
                    games = games.OrderByDescending(g => g.CreatedAt);
                    break;
                case GameStage.Playing:
                    games = games.OrderByDescending(g => g.StartedAt);
                    break;
                case GameStage.Ended:
                    games = games.OrderByDescending(g => g.EndedAt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            else {
                games = games.OrderByDescending(g => g.CreatedAt);
            }
            var gameIds = await games.Select(g => g.Id).Take(count)
                .ToListAsync(cancellationToken);
            return await GetManyAsync(gameIds, cancellationToken);
        }

        // Invalidation

        [CommandHandler(IsFilter = true, Priority = 1)]
        protected virtual async Task OnGameCommandAsync(Game.IGameCommand command, CancellationToken cancellationToken = default)
        {
            // Common invalidation logic for all IGameCommands
            var context = CommandContext.GetCurrent();
            if (!Computed.IsInvalidating()) {
                await context.InvokeRemainingHandlers(cancellationToken);
                return;
            }

            var operationItems = context.Operation().Items;
            var game = operationItems.Get<Game>();
            var prevState = operationItems.GetOrDefault(Box.New(game.Stage)).Value;

            // Invalidation
            FindAsync(game.Id, default).Ignore();

            // Own games of all affected players
            foreach (var gamePlayer in game.Players)
                PseudoListOwnAsync(gamePlayer.UserId.ToString(), default).Ignore();
            var leftPlayer = operationItems.TryGet<GamePlayer>();
            if (leftPlayer != null)
                PseudoListOwnAsync(leftPlayer.UserId.ToString(), default).Ignore();

            // Global lists
            PseudoListAsync(game.EngineId, game.Stage, default).Ignore();
            PseudoListAsync(game.EngineId, null, default).Ignore();
            PseudoListAsync(null, game.Stage, default).Ignore();
            PseudoListAsync(null, null, default).Ignore();
            if (prevState != game.Stage) {
                PseudoListAsync(game.EngineId, prevState, default).Ignore();
                PseudoListAsync(null, prevState, default).Ignore();
            }
        }

        [ComputeMethod]
        protected virtual Task<Unit> PseudoListOwnAsync(string userId, CancellationToken cancellationToken = default)
            => TaskEx.UnitTask;
        [ComputeMethod]
        protected virtual Task<Unit> PseudoListAsync(string? engineId, GameStage? stage, CancellationToken cancellationToken = default)
            => TaskEx.UnitTask;

        // Protected methods

        protected async Task<ImmutableList<Game>> GetManyAsync(IEnumerable<string> gameIds, CancellationToken cancellationToken)
        {
            var result = await gameIds.ParallelSelectToListAsync(FindAsync, cancellationToken);
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
            var dbGame = await dbContext.Games.FindAsync(ComposeKey(id), cancellationToken);
            if (dbGame == null)
                return null;
            await dbContext.Entry(dbGame).Collection(nameof(dbGame.Players))
                .LoadAsync(cancellationToken);
            return dbGame;
        }
    }
}
