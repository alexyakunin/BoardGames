using System;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;

namespace BoardGames.Abstractions
{
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
        public string[] Cells { get; }
        
        public string this[int r, int c] {
            get {
                var cellIndex = GetCellIndex(r, c);
                if (cellIndex < 0 || cellIndex >= Cells.Length)
                    return "    ";
                return Cells[cellIndex];
            }
        }
        
        public DiceBoard(int size)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = new string[size * size];
            for (int i = 0; i < size * size; i++) { Cells[i] = "    "; }
        }
        
        [JsonConstructor]
        public DiceBoard(int size, string[] cells)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (size * size != cells.Length)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = cells;
        }

        public int GetCellIndex(int r, int c) => r * Size + c;

        public DiceBoard Set(int r, int c, int playerIndex, char value)
        {
            if (r < 0 || r >= Size)
                throw new ArgumentOutOfRangeException(nameof(r));
            if (c < 0 || c >= Size)
                throw new ArgumentOutOfRangeException(nameof(c));
            var cellIndex = GetCellIndex(r, c);
            var cell = Cells[cellIndex];
            cell = cell.Substring(0, playerIndex) + value + cell.Substring(playerIndex + 1);
            return new DiceBoard(Size, Cells);
        }
    }
}
