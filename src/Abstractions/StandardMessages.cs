namespace BoardGames.Abstractions
{
    public static class StandardMessages
    {
        public static GameMessage Win(GameUser winner)
            => new(new GameUserMention(winner), new PlainText(" won!"));
        public static GameMessage WinWithScore(GameUser winner, Game game, long score)
            => new(new GameUserMention(winner), new PlainText(" won with "), new GameScoreMention(game, score));
        public static GameMessage MoveTurn(GameUser player)
            => new(new GameUserMention(player), new PlainText(", your turn!"));
    }
}
