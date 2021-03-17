using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Server;
using Stl.Fusion.Authentication;
using BoardGames.Abstractions;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]/[action]")]
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

        [HttpPost]
        public Task<ChatMessage> Post([FromBody] Chat.PostCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Chats.Post(command, cancellationToken);
        }

        [HttpPost]
        public Task Delete([FromBody] Chat.DeleteCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(SessionResolver);
            return Chats.Delete(command, cancellationToken);
        }

        // Queries

        [HttpGet("{chatId}"), Publish]
        public Task<Chat?> TryGet([FromRoute] string chatId, CancellationToken cancellationToken = default)
        {
            chatId = HttpUtility.UrlDecode(chatId);
            return Chats.TryGet(chatId, cancellationToken);
        }

        [HttpGet("{chatId}"), Publish]
        public Task<ChatPermission> GetPermissions(Session? session, [FromRoute] string chatId, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            chatId = HttpUtility.UrlDecode(chatId);
            return Chats.GetPermissions(session, chatId, cancellationToken);
        }

        [HttpGet("{chatId}"), Publish]
        public Task<ChatPage> GetTail(Session? session, [FromRoute] string chatId, int limit, CancellationToken cancellationToken = default)
        {
            session ??= SessionResolver.Session;
            chatId = HttpUtility.UrlDecode(chatId);
            return Chats.GetTail(session, chatId, limit, cancellationToken);
        }

        [HttpGet("{chatId}"), Publish]
        public Task<long> GetMessageCount([FromRoute] string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default)
        {
            chatId = HttpUtility.UrlDecode(chatId);
            return Chats.GetMessageCount(chatId, period, cancellationToken);
        }
    }
}
