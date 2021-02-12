using System.Collections.Immutable;
using System.Linq;
using Stl.Serialization;

namespace BoardGames.Abstractions
{
    public interface IGameEngine
    {
        string Id { get; }
        string Title { get; }
        string Icon { get; }
        int MinPlayerCount { get; }
        int MaxPlayerCount { get; }
        bool AutoStart { get; }

        Game Create(Game game);
        Game Start(Game game);
        Game Move(Game game, GameMove move);
    }

    public abstract class GameEngine<TGameState, TGameMove> : IGameEngine
        where TGameMove : GameMove
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Icon { get; }
        public abstract int MinPlayerCount { get; }
        public abstract int MaxPlayerCount { get; }
        public abstract bool AutoStart { get; }

        public virtual Game Create(Game game) => game;
        public abstract Game Start(Game game);
        Game IGameEngine.Move(Game game, GameMove move) => Move(game, (TGameMove) move);
        public abstract Game Move(Game game, TGameMove move);

        public virtual string SerializeState(TGameState state)
            => JsonSerialized.New(state).SerializedValue;

        public virtual TGameState DeserializeState(string stateJson)
            => JsonSerialized.New<TGameState>(stateJson).Value;

        protected Game SetPlayerScore(Game game, int playerIndex, long score)
            => game with {
                Players = game.Players.Select((p, i) =>
                    i == playerIndex
                        ? p with { Score = score}
                        : p).ToImmutableList(),
            };

        protected Game IncrementPlayerScore(Game game, int playerIndex, long score)
            => game with {
                Players = game.Players.Select((p, i) =>
                    i == playerIndex
                        ? p with { Score = p.Score + score}
                        : p).ToImmutableList(),
            };
    }
}
