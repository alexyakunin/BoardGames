using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using BoardGames.Abstractions;

namespace BoardGames.HostServices
{
    [Index(nameof(EngineId), nameof(Score))]
    public class DbGamePlayer
    {
        public string EngineId { get; set; } = "";
        [Column("GameId")]
        public string DbGameId { get; set; } = "";
        [Column("UserId")]
        public long DbUserId { get; set; }
        public int Index { get; set; }
        public long Score { get; set; }

        public GamePlayer ToModel()
            => new(DbUserId, Score);

        public void UpdateFrom(GamePlayer model, Game game, int index)
        {
            EngineId = game.EngineId;
            DbGameId = game.Id;
            DbUserId = model.UserId;
            Index = index;
            Score = model.Score;
        }
    }
}
