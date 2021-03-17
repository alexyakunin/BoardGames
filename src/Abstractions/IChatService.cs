using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR.Configuration;
using Stl.Fusion;
using Stl.Fusion.Authentication;

namespace BoardGames.Abstractions
{
    public interface IChatService
    {
        // Commands
        [CommandHandler]
        Task<ChatMessage> Post(Chat.PostCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task Delete(Chat.DeleteCommand command, CancellationToken cancellationToken = default);

        // Queries
        [ComputeMethod(KeepAliveTime = 1)]
        Task<Chat?> TryGet(string chatId, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ChatPermission> GetPermissions(Session session, string chatId, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ChatPage> GetTail(Session session, string chatId, int limit, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<long> GetMessageCount(string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default);
    }
}
