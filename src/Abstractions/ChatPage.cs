using System.Collections.Immutable;

namespace BoardGames.Abstractions
{
    public record ChatPage(string ChatId, int Limit)
    {
        public ImmutableList<ChatMessage> Messages { get; init; } = ImmutableList<ChatMessage>.Empty;
        public ImmutableDictionary<long, AppUser> Users { get; init; } = ImmutableDictionary<long, AppUser>.Empty;

        public ChatPage() : this("", 0) { }
    }
}
