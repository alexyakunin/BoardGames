using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace BoardGames.Abstractions
{
    public interface IAppUserService
    {
        // Queries
        [ComputeMethod(KeepAliveTime = 10)]
        Task<AppUser?> TryGet(long id, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<AppUser?> TryGetByName(string name, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsOnline(long id, CancellationToken cancellationToken = default);
    }
}
