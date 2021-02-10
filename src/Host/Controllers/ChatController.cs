using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;
using Stl.Fusion.Authentication;
using BoardGames.Abstractions;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController, JsonifyErrors]
    public class ChatController : ControllerBase, IChatService
    {
        protected IChatService Chats { get; }
        protected ISessionResolver SessionResolver { get; }

        public ChatController(IChatService chats, ISessionResolver sessionResolver)
        {
            Chats = chats;
            SessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost("post")]
        public Task<ChatMessage> PostAsync([FromBody] Chat.PostCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Chats.PostAsync(command, cancellationToken);
        }

        [HttpPost("delete")]
        public Task DeleteAsync([FromBody] Chat.DeleteCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Chats.DeleteAsync(command, cancellationToken);
        }

        // Queries

        [HttpGet("findChat/{id}"), Publish]
        public Task<Chat?> FindChatAsync([FromRoute] string chatId, CancellationToken cancellationToken = default)
            => Chats.FindChatAsync(chatId, cancellationToken);

        [HttpGet("getChatPermissions"), Publish]
        public Task<ChatPermission> GetChatPermissionsAsync(Session? session, string chatId, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Chats.GetChatPermissionsAsync(session, chatId, cancellationToken);
        }

        [HttpGet("getTail"), Publish]
        public Task<ChatPage> GetTailAsync(Session? session, string chatId, int limit, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            return Chats.GetTailAsync(session, chatId, limit, cancellationToken);
        }

        [HttpGet("getMessageCount"), Publish]
        public Task<long> GetMessageCountAsync(string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default)
            => Chats.GetMessageCountAsync(chatId, period, cancellationToken);
    }
}
