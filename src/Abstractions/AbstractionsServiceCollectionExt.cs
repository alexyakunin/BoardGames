using BoardGames.Abstractions.Games;

namespace BoardGames.Abstractions;

public static class AbstractionsServiceCollectionExt
{
    public static IServiceCollection AddGameEngines(this IServiceCollection services)
    {
        services.AddSingleton<IGameEngine, GomokuEngine>();
        services.AddSingleton<IGameEngine, RpsEngine>();
        services.AddSingleton(c =>
            c.GetRequiredService<IEnumerable<IGameEngine>>().ToImmutableDictionary(e => e.Id));
        return services;
    }
}
