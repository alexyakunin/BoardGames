namespace BoardGames.Abstractions
{
    public record GameUser(long Id, string Name = "(unknown)")
    {
        public static GameUser None { get; } = new();

        public GameUser() : this(0, "(none)") { }
    }
}
