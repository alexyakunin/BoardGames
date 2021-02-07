namespace BoardGames.Abstractions
{
    public static class GameMessages
    {
        public static string Win(int playerIndex)
            => $"|player_{playerIndex}| won!";
        public static string WinWithScore(int playerIndex)
            => $"|player_{playerIndex}| won with |score_{playerIndex}|!";
        public static string MoveTurn(int playerIndex)
            => $"|player_{playerIndex}|, your move!";
    }
}
