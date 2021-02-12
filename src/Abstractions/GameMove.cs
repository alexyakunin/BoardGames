using Stl.Time;

namespace BoardGames.Abstractions
{
    public abstract record GameMove
    {
        public int PlayerIndex { get; init; }
        public Moment Time { get; init; }
    }
}
