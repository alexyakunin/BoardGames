using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Runtime.CompilerServices;
using Stl.Fusion.Authentication;

namespace BoardGames.Abstractions
{
    public record Game
    {
        public interface IGameCommand : ISessionCommand { }
        public interface IGameCommand<TResult> : ISessionCommand<TResult>, IGameCommand { }

        public record CreateCommand(Session Session, string EngineId) : IGameCommand<Game> {
            public CreateCommand() : this(Session.Null, "") { }
        }
        public record JoinCommand(Session Session, string Id, bool Join = true) : IGameCommand<Unit> {
            public JoinCommand() : this(Session.Null, "") { }
        }
        public record StartCommand(Session Session, string Id) : IGameCommand<Unit> {
            public StartCommand() : this(Session.Null, "") { }
        }
        public record MoveCommand(Session Session, string Id, GameMove Move) : IGameCommand<Unit> {
            public MoveCommand() : this(Session.Null, "", null!) { }
        }
        public record EditCommand(Session Session,
            string Id,
            bool? IsPublic = null,
            string? Intro = null
        ) : IGameCommand<Unit> {
            public EditCommand() : this(Session.Null, "") { }
        }

        public string Id { get; init; } = "";
        public string EngineId { get; init; } = "";
        public long UserId { get; init; }
        public bool IsPublic { get; init; }
        public string Intro { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? LastMoveAt { get; init; }
        public DateTime? EndedAt { get; init; }
        public long? MaxScore { get; init; }
        public GameStage Stage { get; init; }
        public string StateMessage { get; init; } = "";
        public string StateJson { get; init; } = "";
        public ImmutableList<GamePlayer> Players { get; init; } = ImmutableList<GamePlayer>.Empty;

        public virtual bool Equals(Game? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
