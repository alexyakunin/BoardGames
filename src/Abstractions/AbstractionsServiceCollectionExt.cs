using BoardGames.Abstractions.Games;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BoardGames.Abstractions;

public static class AbstractionsServiceCollectionExt
{
    public static IServiceCollection AddGameEngines(this IServiceCollection services)
    {
        // TryAddEnumerable makes this method idempotent
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, GomokuEngine>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, TicTacToeEngine>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, ConnectFourEngine>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, ReversiEngine>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, RpsEngine>());
        services.TryAddSingleton(c =>
            c.GetRequiredService<IEnumerable<IGameEngine>>().ToImmutableDictionary(e => e.Id));
        return services;
    }
}
