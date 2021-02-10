using Microsoft.EntityFrameworkCore;
using BoardGames.Abstractions;

namespace BoardGames.HostServices
{
    [Index(nameof(EngineId), nameof(Score))]
    public class DbGamePlayer
    {
        public string EngineId { get; set; } = "";
        public string GameId { get; set; } = "";
        public long UserId { get; set; }
        public int Index { get; set; }
        public long Score { get; set; }

        public GamePlayer ToModel()
            => new(UserId, Score);

        public void UpdateFrom(GamePlayer source, Game game, int index)
        {
            EngineId = game.EngineId;
            GameId = game.Id;
            UserId = source.UserId;
            Index = index;
            Score = source.Score;
        }
    }
}
