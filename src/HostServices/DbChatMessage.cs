using System;
using System.ComponentModel.DataAnnotations;
using BoardGames.Abstractions;
using Microsoft.EntityFrameworkCore;
using Stl.Time;

namespace BoardGames.HostServices
{
    [Index(nameof(ChatId), nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(CreatedAt), nameof(ChatId))]
    [Index(nameof(UserId), nameof(ChatId), nameof(CreatedAt))]
    [Index(nameof(ChatId), nameof(Id), nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(Id), nameof(CreatedAt), nameof(ChatId))]
    [Index(nameof(UserId), nameof(ChatId), nameof(Id), nameof(CreatedAt))]
    public class DbChatMessage
    {
        private DateTime _createdAt;
        private DateTime _editedAt;

        [Key] public string Id { get; set; } = "";
        public string ChatId { get; set; } = "";
        public long UserId { get; set; }
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
            => new(Id, ChatId) {
                UserId = UserId,
                CreatedAt = CreatedAt,
                EditedAt = EditedAt,
                IsRemoved = IsRemoved,
                Text = Text,
            };

        public void UpdateFrom(ChatMessage model)
        {
            if (string.IsNullOrEmpty(Id))
                Id = model.Id;
            ChatId = model.ChatId;
            UserId = model.UserId;
            CreatedAt = model.CreatedAt;
            EditedAt = model.EditedAt;
            IsRemoved = model.IsRemoved;
            Text = model.Text;
        }
    }
}
