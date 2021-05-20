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
    [RegisterRestEaseReplicaService(typeof(IGameService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("game")]
    public interface IGameServiceClient
    {
        // Commands
        [Post("create")]
        Task<Game> Create([Body] Game.CreateCommand command, CancellationToken cancellationToken = default);
        [Post("join")]
        Task Join([Body] Game.JoinCommand command, CancellationToken cancellationToken = default);
        [Post("start")]
        Task Start([Body] Game.StartCommand command, CancellationToken cancellationToken = default);
        [Post("move")]
        Task Move([Body] Game.MoveCommand command, CancellationToken cancellationToken = default);
        [Post("edit")]
        Task Edit([Body] Game.EditCommand command, CancellationToken cancellationToken = default);

        // Queries
        [Get("tryGet/{id}")]
        Task<Game?> TryGet([Path] string id, CancellationToken cancellationToken = default);
        [Get("listOwn")]
        Task<ImmutableList<Game>> ListOwn(string? engineId, GameStage? stage, int count, Session session, CancellationToken cancellationToken = default);
        [Get("list")]
        Task<ImmutableList<Game>> List(string? engineId, GameStage? stage, int count, CancellationToken cancellationToken = default);
    }

    [RegisterRestEaseReplicaService(typeof(IAppUserService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("appUser")]
    public interface IAppUserServiceClient
    {
        // Queries
        [Get("tryGet/{id}")]
        Task<AppUser?> TryGet([Path] long id, CancellationToken cancellationToken = default);
        [Get("tryGetByName/{name}")]
        Task<AppUser?> TryGetByName([Path] string name, CancellationToken cancellationToken = default);
        [Get("isOnline/{id}")]
        Task<bool> IsOnline([Path] long id, CancellationToken cancellationToken = default);
    }

    [RegisterRestEaseReplicaService(typeof(IChatService), Scope = ServiceScope.ClientSideOnly)]
    [BasePath("chat")]
    public interface IChatServiceClient
    {
        // Commands
        [Post("post")]
        Task<ChatMessage> Post([Body] Chat.PostCommand command, CancellationToken cancellationToken = default);
        [Post("delete")]
        Task Delete([Body] Chat.DeleteCommand command, CancellationToken cancellationToken = default);

        // Queries
        [Get("tryGet/{chatId}")]
        Task<Chat?> TryGet([Path] string chatId, CancellationToken cancellationToken = default);
        [Get("getPermissions/{chatId}")]
        Task<ChatPermission> GetPermissions(Session session, [Path] string chatId, CancellationToken cancellationToken = default);
        [Get("getTail/{chatId}")]
        Task<ChatPage> GetTail(Session session, [Path] string chatId, int limit, CancellationToken cancellationToken = default);
        [Get("getMessageCount/{chatId}")]
        Task<long> GetMessageCount([Path] string chatId, TimeSpan? period = null, CancellationToken cancellationToken = default);
    }
}
