namespace BoardGames.Abstractions;

public interface IMessageParser : IComputeService
{
    [ComputeMethod(MinCacheDuration = 1)]
    public Task<GameMessage> Parse(string text, CancellationToken cancellationToken = default);
}
