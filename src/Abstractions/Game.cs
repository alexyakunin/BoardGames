using System.Runtime.CompilerServices;
using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public partial record Game
{
    public string Id { get; init; } = "";
    public string EngineId { get; init; } = "";
    public long UserId { get; init; }
    public bool IsPublic { get; init; }
    public int? RoundCount { get; init; }
    public int RoundIndex { get; init; }
    public string Intro { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? LastMoveAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public long? MaxScore { get; init; }
    public GameStage Stage { get; init; }
    public string StateMessage { get; init; } = "";
    public string StateJson { get; init; } = "";
    public ImmutableList<GamePlayer> Players { get; init; } = ImmutableList<GamePlayer>.Empty;

    public virtual bool Equals(Game? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
