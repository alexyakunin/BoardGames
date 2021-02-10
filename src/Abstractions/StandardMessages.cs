namespace BoardGames.Abstractions
{
    public static class StandardMessages
    {
        public static string Win(AppUser winner)
            => new GameMessage(
                new UserMention(winner),
                new PlainText(" won!")
            ).Format();
        public static string WinWithScore(AppUser winner, Game game, long score)
            => new GameMessage(
                new UserMention(winner),
                new PlainText(" won with "),
                new GameScoreMention(game, score)
            ).Format();
        public static string MoveTurn(AppUser player)
            => new GameMessage
                (new UserMention(player),
                new PlainText(", your turn!")
            ).Format();
    }
}
