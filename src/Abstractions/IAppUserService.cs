namespace BoardGames.Abstractions;

public interface IAppUserService : IComputeService
{
    // Queries
    [ComputeMethod(MinCacheDuration = 10)]
    Task<AppUser?> TryGet(long id, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<AppUser?> TryGetByName(string name, CancellationToken cancellationToken = default);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<bool> IsOnline(long id, CancellationToken cancellationToken = default);
}
