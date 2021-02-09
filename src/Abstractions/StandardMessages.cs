namespace BoardGames.Abstractions
{
    public static class StandardMessages
    {
        public static string Win(GameUser winner)
            => new GameMessage(
                new GameUserMention(winner),
                new PlainText(" won!")
            ).Format();
        public static string WinWithScore(GameUser winner, Game game, long score)
            => new GameMessage(
                new GameUserMention(winner),
                new PlainText(" won with "),
                new GameScoreMention(game, score)
            ).Format();
        public static string MoveTurn(GameUser player)
            => new GameMessage
                (new GameUserMention(player),
                new PlainText(", your turn!")
            ).Format();
    }
}
