using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace BoardGames.Abstractions
{
    public interface IMessageParser
    {
        [ComputeMethod(KeepAliveTime = 1)]
        public Task<GameMessage> ParseAsync(string text, CancellationToken cancellationToken = default);
    }
}
