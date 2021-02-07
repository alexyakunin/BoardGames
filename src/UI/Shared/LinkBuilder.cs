namespace BoardGames.UI.Shared
{
    public static class LinkBuilder
    {
        public static string Home() => "/";
        public static string Game(string engineId, string gameId = "") => $"/game/{engineId}/{gameId}";
        public static string Profile() => "/profile";
        public static string SourceCode() => "https://github.com/alexyakunin/BoardGames";
    }
}
