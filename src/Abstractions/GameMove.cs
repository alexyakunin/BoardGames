using Stl.Time;

namespace BoardGames.Abstractions
{
    public abstract record GameMove(Moment Time)
    {
        protected GameMove() : this(default(Moment)) { }
    }
}
