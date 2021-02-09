using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace BoardGames.Abstractions
{
    public interface IGameUserService
    {
        // Queries
        [ComputeMethod(KeepAliveTime = 10)]
        Task<GameUser?> FindAsync(long id, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<GameUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 10)]
        Task<bool> IsOnlineAsync(long id, CancellationToken cancellationToken = default);
    }
}
