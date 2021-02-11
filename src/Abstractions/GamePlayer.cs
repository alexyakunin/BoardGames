namespace BoardGames.Abstractions
{
    public record GamePlayer(long UserId, long Score = 0)
    {
        public GamePlayer() : this(0) { }
    }
}
