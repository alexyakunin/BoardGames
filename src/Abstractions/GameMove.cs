using BoardGames.Abstractions.Games;
using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
[Union(0, typeof(MnkGameMove))]
[Union(1, typeof(RpsMove))]
[Union(2, typeof(ConnectFourMove))]
[Union(3, typeof(ReversiMove))]
public abstract partial record GameMove
{
    public int PlayerIndex { get; init; }
    public Moment Time { get; init; }
}
