using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BoardGames.Abstractions;
using Stl.Time;

namespace BoardGames.HostServices
{
    [Table("Games")]
    [Index(nameof(Stage), nameof(IsPublic), nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(Stage), nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(CreatedAt), nameof(Stage))]
    public class DbGame
    {
        private DateTime _createdAt;
        private DateTime? _startedAt;
        private DateTime? _lastMoveAt;
        private DateTime? _endedAt;

        [Key] public string Id { get; set; } = "";
        public string EngineId { get; set; } = "";
        public long UserId { get; set; }
        public bool IsPublic { get; set; }
        public string Intro { get; set; } = "";

        public List<DbGamePlayer> Players { get; set; } = new();

        public DateTime CreatedAt {
            get => _createdAt.DefaultKind(DateTimeKind.Utc);
            set => _createdAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public DateTime? StartedAt {
            get => _startedAt.DefaultKind(DateTimeKind.Utc);
            set => _startedAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public DateTime? LastMoveAt {
            get => _lastMoveAt.DefaultKind(DateTimeKind.Utc);
            set => _lastMoveAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public DateTime? EndedAt {
            get => _endedAt.DefaultKind(DateTimeKind.Utc);
            set => _endedAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public long? MaxScore { get; set; }
        public GameStage Stage { get; set; }
        public string StateMessage { get; set; } = "";
        public string StateJson { get; set; } = "";

        public Game ToModel()
            => new() {
                Id = Id,
                EngineId = EngineId,
                UserId = UserId,
                IsPublic = IsPublic,
                Intro = Intro,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                LastMoveAt = LastMoveAt,
                EndedAt = EndedAt,
                MaxScore = MaxScore,
                Stage = Stage,
                StateMessage = StateMessage,
                StateJson = StateJson,
                Players = Players.OrderBy(p => p.Index).Select(p => p.ToModel()).ToImmutableList(),
            };

        public void UpdateFrom(Game source)
        {
            if (string.IsNullOrEmpty(Id)) {
                Id = source.Id;
                EngineId = source.EngineId;
                UserId = source.UserId;
                CreatedAt = source.CreatedAt;
            }
            IsPublic = source.IsPublic;
            Intro = source.Intro;
            StartedAt = source.StartedAt;
            LastMoveAt = source.StartedAt;
            EndedAt = source.EndedAt;
            MaxScore = source.MaxScore;
            Stage = source.Stage;
            StateMessage = source.StateMessage;
            StateJson = source.StateJson;

            var players = source.Players.ToDictionary(p => p.UserId);
            var dbPlayers = Players.Where(p => players.ContainsKey(p.UserId)).ToDictionary(p => p.UserId);
            Players = new List<DbGamePlayer>();
            var playerIndex = 0;
            foreach (var player in source.Players) {
                var dbPlayer = dbPlayers.GetValueOrDefault(player.UserId) ?? new DbGamePlayer();
                dbPlayer.UpdateFrom(player, source, playerIndex);
                Players.Add(dbPlayer);
                playerIndex++;
            }
        }
    }
}
