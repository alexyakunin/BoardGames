using System.Threading;
using System.Threading.Tasks;

namespace BoardGames.Abstractions
{
    public interface IMessageParser
    {
        public ValueTask<GameMessage> ParseAsync(string text, CancellationToken cancellationToken = default);
    }
}
