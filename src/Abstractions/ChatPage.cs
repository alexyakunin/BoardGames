using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public sealed partial record ChatPage(string ChatId, int Limit)
{
    public static ChatPage None { get; } = new("", 0);

    public ImmutableList<ChatMessage> Messages { get; init; } = ImmutableList<ChatMessage>.Empty;
    public ImmutableDictionary<long, AppUser> Users { get; init; } = ImmutableDictionary<long, AppUser>.Empty;
}
