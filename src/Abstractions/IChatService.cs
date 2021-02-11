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
        Task<ChatMessage> PostAsync(Chat.PostCommand command, CancellationToken cancellationToken = default);
        [CommandHandler]
        Task DeleteAsync(Chat.DeleteCommand command, CancellationToken cancellationToken = default);

        // Queries
        [ComputeMethod(KeepAliveTime = 1)]
        Task<Chat?> FindChatAsync(string chatId, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ChatPermission> GetChatPermissionsAsync(Session session, string chatId, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<ChatPage> GetTailAsync(Session session, string chatId, int limit, CancellationToken cancellationToken = default);
        [ComputeMethod(KeepAliveTime = 1)]
        Task<long> GetMessageCountAsync(string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default);
    }
}
