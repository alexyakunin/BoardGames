using System.Text.Json.Serialization;

namespace BoardGames.Abstractions;

public record CharBoard
{
    private static readonly ConcurrentDictionary<(int Width, int Height), CharBoard> EmptyCache = new();

    public static CharBoard Empty(int size) => Empty(size, size);
    public static CharBoard Empty(int width, int height)
        => EmptyCache.GetOrAdd((width, height), key => new CharBoard(key.Width, key.Height));

    public int Width { get; }
    public int Height { get; }
    public string Cells { get; }

    [JsonIgnore]
    public bool IsFull => !Cells.Contains(' ');

    [JsonIgnore]
    public char this[int r, int c] {
        get {
            if (r < 0 || r >= Height || c < 0 || c >= Width)
                return ' ';
            return Cells[GetCellIndex(r, c)];
        }
    }

    [JsonIgnore]
    public string this[int r] {
        get {
            if (r < 0 || r >= Height)
                return "";
            return Cells.Substring(GetCellIndex(r, 0), Width);
        }
    }

    public CharBoard(int width, int height)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        Cells = new string(' ', width * height);
    }

    [JsonConstructor]
    public CharBoard(int width, int height, string cells)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (width * height != cells.Length)
            throw new ArgumentOutOfRangeException(nameof(cells));
        Width = width;
        Height = height;
        Cells = cells;
    }

    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.AppendFormat("Cells[{0}x{1}] = [\r\n", Width, Height);
        for (var rowIndex = 0; rowIndex < Height; rowIndex++)
            builder.AppendFormat("  |{0}|\r\n", this[rowIndex]);
        builder.Append("  ]");
        return true;
    }

    public int GetCellIndex(int r, int c) => r * Width + c;

    public int Count(char value)
        => Cells.Count(c => c == value);

    public CharBoard Set(int r, int c, char value)
    {
        if (r < 0 || r >= Height)
            throw new ArgumentOutOfRangeException(nameof(r));
        if (c < 0 || c >= Width)
            throw new ArgumentOutOfRangeException(nameof(c));
        var cellIndex = GetCellIndex(r, c);
        var newCells = Cells.Substring(0, cellIndex) + value + Cells.Substring(cellIndex + 1);
        return new CharBoard(Width, Height, newCells);
    }
}
