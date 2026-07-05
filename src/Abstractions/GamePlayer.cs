using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public sealed partial record GamePlayer(long UserId, long Score = 0);
