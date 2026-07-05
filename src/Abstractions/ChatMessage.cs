using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public sealed partial record ChatMessage(string Id, string ChatId)
{
    public long UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime EditedAt { get; init; }
    public bool IsRemoved { get; init; }
    public string Text { get; init; } = "";
}
