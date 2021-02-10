using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace BoardGames.Abstractions
{
    public interface IAppUserService
    {
        // Queries
        [ComputeMethod(KeepAliveTime = 10)]
        Task<AppUser?> FindAsync(long id, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<AppUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsOnlineAsync(long id, CancellationToken cancellationToken = default);
    }
}
