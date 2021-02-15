using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using Stl.Time;

namespace BoardGames.HostServices
{
    [Table("ChatMessages")]
    [Index(nameof(DbChatId), nameof(CreatedAt))]
    [Index(nameof(DbUserId), nameof(CreatedAt), nameof(DbChatId))]
    [Index(nameof(DbUserId), nameof(DbChatId), nameof(CreatedAt))]
    [Index(nameof(DbChatId), nameof(Id), nameof(CreatedAt))]
    [Index(nameof(DbUserId), nameof(Id), nameof(CreatedAt), nameof(DbChatId))]
    [Index(nameof(DbUserId), nameof(DbChatId), nameof(Id), nameof(CreatedAt))]
    public class DbChatMessage
    {
        private DateTime _createdAt;
        private DateTime _editedAt;

        [Key] public string Id { get; set; } = "";
        [Column("ChatId")]
        public string DbChatId { get; set; } = "";
        [Column("UserId")]
        public long DbUserId { get; set; }
        public bool IsRemoved { get; set; }

        public DateTime CreatedAt {
            get => _createdAt.DefaultKind(DateTimeKind.Utc);
            set => _createdAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public DateTime EditedAt {
            get => _editedAt.DefaultKind(DateTimeKind.Utc);
            set => _editedAt = value.DefaultKind(DateTimeKind.Utc);
        }

        public string Text { get; set; } = "";

        public ChatMessage ToModel()
            => new(Id, DbChatId) {
                UserId = DbUserId,
                CreatedAt = CreatedAt,
                EditedAt = EditedAt,
                IsRemoved = IsRemoved,
                Text = Text,
            };

        public void UpdateFrom(ChatMessage model)
        {
            if (string.IsNullOrEmpty(Id))
                Id = model.Id;
            DbChatId = model.ChatId;
            DbUserId = model.UserId;
            CreatedAt = model.CreatedAt;
            EditedAt = model.EditedAt;
            IsRemoved = model.IsRemoved;
            Text = model.Text;
        }
    }
}
