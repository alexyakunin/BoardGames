using BoardGames.Abstractions.Games;
using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
[Union(0, typeof(GomokuMove))]
[Union(1, typeof(RpsMove))]
public abstract partial record GameMove
{
    public int PlayerIndex { get; init; }
    public Moment Time { get; init; }
}
