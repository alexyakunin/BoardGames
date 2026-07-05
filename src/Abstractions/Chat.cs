using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public sealed partial record Chat(string Id, ChatKind Kind)
{
    public ImmutableHashSet<long> OwnerIds { get; init; } = ImmutableHashSet<long>.Empty;
    public ImmutableHashSet<long> ParticipantIds { get; init; } = ImmutableHashSet<long>.Empty;
    public bool IsPublic { get; init; }

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
}
