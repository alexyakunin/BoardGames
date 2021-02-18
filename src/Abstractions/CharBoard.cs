using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
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
        public ImmutableDictionary<int, Cell> Cells { get; }
        
        public struct Cell
        {
            public string Background;
            public string[] Colors;
            public double[] Opacities;

            public Cell(string background, string[] colors, double[] opacities)
            {
                var defaultBackground = GetColor(DiceBoard.Colors.Gold);
                var defaultColors = new string[] {GetColor(DiceBoard.Colors.Blue),
                    GetColor(DiceBoard.Colors.Green),
                    GetColor(DiceBoard.Colors.Red),
                    GetColor(DiceBoard.Colors.Yellow), };
                var defaultOpacities = new double[] {
                    GetOpacity(Opacity.Invisible), GetOpacity(Opacity.Invisible), GetOpacity(Opacity.Invisible),
                    GetOpacity(Opacity.Invisible),
                };
                background ??= defaultBackground;
                colors ??= defaultColors;
                opacities ??= defaultOpacities;
                Background = background;
                Colors = colors;
                Opacities = opacities;
            }
        }
        
        public enum Colors
        {
            Blue,
            Green,
            Red,
            Yellow,
            Gold,
            Forward,
            Backward,
        }

        public enum Opacity
        {
            Current,
            Past,
            Invisible
        }
        
        public Cell this[int r, int c] {
            get {
                var cellIndex = GetCellIndex(r, c);
                if (cellIndex < 0 || cellIndex >= Cells.Count)
                    return new Cell {};
                return Cells[cellIndex];
            }
        }
        
        public DiceBoard(int size)
        {
            var opacity = GetOpacity(Opacity.Invisible);
            var background = GetColor(Colors.Gold);
            var p1 = GetColor(Colors.Blue);
            var p2 = GetColor(Colors.Green);
            var p3 = GetColor(Colors.Red);
            var p4 = GetColor(Colors.Yellow);
            var colors = new string[] {p1, p2, p3, p4};
            var opacities = new double[] {opacity, opacity, opacity, opacity };
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            var builder = ImmutableDictionary.CreateBuilder<int, Cell>();
            for (int i = 0; i < size * size; i++) {
                if (i == 10 || i == 27 || i == 44) {   // ForwardStep cells
                    builder.Add(i, new Cell() {Background = GetColor(Colors.Forward), Colors = colors, Opacities = opacities});
                } else if (i == 20 || i == 35 || i == 54) {    // BackwardStep cells
                    builder.Add(i, new Cell() {Background = GetColor(Colors.Backward), Colors = colors, Opacities = opacities});
                }
                else {
                    builder.Add(i, new Cell() {Background = GetColor(Colors.Gold), Colors = colors, Opacities = opacities});
                }
            }
            Cells = builder.ToImmutable();
        }
        
        [JsonConstructor]
        public DiceBoard(int size, ImmutableDictionary<int, Cell> cells)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (size * size != cells.Count)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Cells = cells;
        }

        public int GetCellIndex(int r, int c) => r * Size + c;

        public DiceBoard Set(int r, int c, int playerIndex, double value)
        {
            if (r < 0 || r >= Size)
                throw new ArgumentOutOfRangeException(nameof(r));
            if (c < 0 || c >= Size)
                throw new ArgumentOutOfRangeException(nameof(c));
            var cellIndex = GetCellIndex(r, c);
            var cell = Cells[cellIndex];
            cell.Opacities[playerIndex] = value;
            return new DiceBoard(Size, Cells);
        }

        public static double GetOpacity(Opacity opacity)
        {
            var results = new Dictionary<Opacity, double>() {
                {Opacity.Current, 1.0},
                {Opacity.Past, 0.1},
                {Opacity.Invisible, 0.0},
            };
            return results[opacity];
        }
        
        public static string GetColor(Colors color)
        {
            var results = new Dictionary<Colors, string>() {
                {Colors.Blue, "blue"},
                {Colors.Green, "green"},
                {Colors.Red, "red"},
                {Colors.Yellow, "yellow"},
                {Colors.Gold, "lightgoldenrodyellow"},
                {Colors.Backward, "#DC381F"},
                {Colors.Forward, "#52D017"},
            };
            return results[color];
        }
    }
}
