using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;

namespace BoardGames.Abstractions
{
    public interface IMessageParser
    {
        [ComputeMethod(KeepAliveTime = 1)]
        public Task<GameMessage> Parse(string text, CancellationToken cancellationToken = default);
    }
}
