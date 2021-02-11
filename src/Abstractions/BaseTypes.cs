using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Stl.Serialization;
using Stl.Time;

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

    public abstract record GameMove(Moment Time)
    {
        protected GameMove() : this(default(Moment)) { }
    }

    public record CharBoard
    {
        private static readonly ConcurrentDictionary<int, CharBoard> EmptyCache = new();
        public static CharBoard Empty(int size) => EmptyCache.GetOrAdd(size, size1 => new CharBoard(size1));

        public int Size { get; }
        public string Cells { get; }

        public char this[int r, int c] {
            get {
                var cellIndex = GetCellIndex(r, c);
                if (cellIndex < 0 || cellIndex >= Cells.Length)
                    return ' ';
                return Cells[cellIndex];
            }
        }

        public string this[int r] {
            get {
                var startIndex = GetCellIndex(r, 0);
                if (startIndex < 0 || startIndex >= Cells.Length)
                    return "";
                return Cells.Substring(startIndex, Size);
            }
        }

        public CharBoard(int size)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = new string(' ', size * size);
        }

        [JsonConstructor]
        public CharBoard(int size, string cells)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (size * size != cells.Length)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = cells;
        }

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.AppendFormat("Cells[{0}x{0}] = [\r\n", Size);
            for (var rowIndex = 0; rowIndex < Size; rowIndex++)
                builder.AppendFormat("  |{0}|\r\n", this[rowIndex]);
            builder.Append("  ]");
            return true;
        }

        public int GetCellIndex(int r, int c) => r * Size + c;

        public CharBoard Set(int r, int c, char value)
        {
            if (r < 0 || r >= Size)
                throw new ArgumentOutOfRangeException(nameof(r));
            if (c < 0 || c >= Size)
                throw new ArgumentOutOfRangeException(nameof(c));
            var cellIndex = GetCellIndex(r, c);
            var newCells = Cells.Substring(0, cellIndex) + value + Cells.Substring(cellIndex + 1);
            return new CharBoard(Size, newCells);
        }
    }
    
    public record DiceBoard
    {
        private static readonly ConcurrentDictionary<int, DiceBoard> EmptyCache = new();
        public static DiceBoard Empty(int size) => EmptyCache.GetOrAdd(size, size1 => new DiceBoard(size1));

        public int Size { get; }
        public Dictionary<int, string[]> Cells { get; }

        public string[] this[int r, int c] {
            get {
                var cellIndex = GetCellIndex(r, c);
                if (cellIndex < 0 || cellIndex >= Cells.Count)
                    return new string[] {"lightblue", "lightblue", "lightblue", "lightblue"};
                return Cells[cellIndex];
            }
        }

        public DiceBoard(int size)
        {
            var defaultValue = "lightblue";
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = new Dictionary<int, string[]>();
            for (int i = 0; i < size * size; i++) {
                Cells[i] = new string[] {defaultValue, defaultValue, defaultValue, defaultValue};
            }
        }

        [JsonConstructor]
        public DiceBoard(int size, Dictionary<int, string[]> cells)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (size * size != cells.Count)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = cells;
        }

        public int GetCellIndex(int r, int c) => r * Size + c;

        public DiceBoard Set(int r, int c, int playerIndex, string value)
        {
            if (r < 0 || r >= Size)
                throw new ArgumentOutOfRangeException(nameof(r));
            if (c < 0 || c >= Size)
                throw new ArgumentOutOfRangeException(nameof(c));
            var cellIndex = GetCellIndex(r, c);
            Cells[cellIndex][playerIndex] = value;
            return new DiceBoard(Size, Cells);
        }
    }
}
