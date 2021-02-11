namespace BoardGames.Abstractions
{
    public record AppUser(long Id, string Name = "(unknown)")
    {
        public static AppUser None { get; } = new();

        public AppUser() : this(0, "(none)") { }
    }
}
