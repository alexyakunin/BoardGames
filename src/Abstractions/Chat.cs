using System;
using System.Collections.Immutable;
using System.Reactive;
using Stl.Fusion.Authentication;

namespace BoardGames.Abstractions
{
    public record Chat(string Id, ChatKind Kind)
    {
        public interface IChatCommand : ISessionCommand
        {
            string ChatId { get; }
        }
        public interface IChatCommand<TResult> : ISessionCommand<TResult>, IChatCommand { }

        public record PostCommand(Session Session, string ChatId, string Text) : IChatCommand<ChatMessage> {
            public PostCommand() : this(Session.Null, "", "") { }
        }
        public record DeleteCommand(Session Session, string ChatId, string MessageId) : IChatCommand<Unit> {
            public DeleteCommand() : this(Session.Null, "", "") { }
        }

        public static string GetGameChatId(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
                throw new ArgumentOutOfRangeException(nameof(gameId));
            return $"game/{gameId}";
        }

        public static string GetP2PChatId(long user1Id, long user2Id)
        {
            if (user1Id == user2Id)
                throw new ArgumentOutOfRangeException(nameof(user2Id));
            var lowId = Math.Min(user1Id, user2Id);
            var highId = Math.Max(user1Id, user2Id);
            return $"p2p/{lowId}/{highId}";
        }

        public ImmutableHashSet<long> OwnerIds { get; init; } = ImmutableHashSet<long>.Empty;
        public ImmutableHashSet<long> ParticipantIds { get; init; } = ImmutableHashSet<long>.Empty;
        public bool IsPublic { get; init; }

        public Chat() : this("", ChatKind.Unknown) { }
    }
}
