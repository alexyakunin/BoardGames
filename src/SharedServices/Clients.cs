using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Client;
using BoardGames.Abstractions;
using Stl.Fusion.Authentication;

namespace BoardGames.ClientServices
{
    [RestEaseReplicaService(typeof(IGameService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("game")]
    public interface IGameServiceClient
    {
        // Commands
        [Post("create")]
        Task<Game> CreateAsync([Body] Game.CreateCommand command, CancellationToken cancellationToken = default);
        [Post("join")]
        Task JoinAsync([Body] Game.JoinCommand command, CancellationToken cancellationToken = default);
        [Post("start")]
        Task StartAsync([Body] Game.StartCommand command, CancellationToken cancellationToken = default);
        [Post("move")]
        Task MoveAsync([Body] Game.MoveCommand command, CancellationToken cancellationToken = default);
        [Post("edit")]
        Task EditAsync([Body] Game.EditCommand command, CancellationToken cancellationToken = default);

        // Queries
        [Get("find/{id}")]
        Task<Game?> FindAsync([Path] string id, CancellationToken cancellationToken = default);
        [Get("listOwn")]
        Task<ImmutableList<Game>> ListOwnAsync(string? engineId, GameStage? stage, int count, Session session, CancellationToken cancellationToken = default);
        [Get("list")]
        Task<ImmutableList<Game>> ListAsync(string? engineId, GameStage? stage, int count, CancellationToken cancellationToken = default);
    }

    [RestEaseReplicaService(typeof(IAppUserService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("appUser")]
    public interface IAppUserServiceClient
    {
        // Queries
        [Get("find/{id}")]
        Task<AppUser?> FindAsync([Path] long id, CancellationToken cancellationToken = default);
        [Get("findByName/{name}")]
        Task<AppUser?> FindByNameAsync([Path] string name, CancellationToken cancellationToken = default);
        [Get("isOnline/{id}")]
        Task<bool> IsOnlineAsync([Path] long id, CancellationToken cancellationToken = default);
    }

    [RestEaseReplicaService(typeof(IChatService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("chat")]
    public interface IChatServiceClient
    {
        // Commands
        [Post("post")]
        Task<ChatMessage> PostAsync([Body] Chat.PostCommand command, CancellationToken cancellationToken = default);
        [Post("delete")]
        Task DeleteAsync([Body] Chat.DeleteCommand command, CancellationToken cancellationToken = default);

        // Queries
        [Get("findChat/{chatId}")]
        Task<Chat?> FindChatAsync([Path] string chatId, CancellationToken cancellationToken = default);
        [Get("getChatPermissions")]
        Task<ChatPermission> GetChatPermissionsAsync(Session session, string chatId, CancellationToken cancellationToken = default);
        [Get("getTail")]
        Task<ChatPage> GetTailAsync(Session session, string chatId, int limit, CancellationToken cancellationToken = default);
        [Get("getMessageCount")]
        Task<long> GetMessageCountAsync(string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default);
    }
}
