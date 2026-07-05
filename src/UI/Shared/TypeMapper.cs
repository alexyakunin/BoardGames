namespace BoardGames.UI;

// AOT-friendly Type -> Type mapping, modeled on ActualChat's TypeMap/TypeMapper:
// all mappings are registered explicitly, so no assembly scanning is involved.

/// <summary>
/// A dictionary mapping source types to component types within the
/// <typeparamref name="TScope"/> scope.
/// </summary>
public sealed class TypeMap<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TScope>
{
    public Dictionary<Type, Type> Map { get; } = new();

    public TypeMap<TScope> Add<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        where TValue : TScope
    {
        Map.Add(typeof(TKey), typeof(TValue));
        return this;
    }
}

/// <summary>
/// Resolves mapped types from a <see cref="TypeMap{TScope}"/> with caching;
/// the lookup walks the base type chain of the source type.
/// </summary>
public sealed class TypeMapper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TScope>
{
    private readonly Dictionary<Type, Type> _map;
    private readonly ConcurrentDictionary<Type, Type?> _cache = new();

    public TypeMapper(TypeMap<TScope> typeMap)
        => _map = typeMap.Map;

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type Get(Type source)
        => TryGet(source)
            ?? throw new KeyNotFoundException(
                $"No matching {typeof(TScope).Name} is found for type '{source.Name}'.");

    public Type? TryGet(Type source)
        => _cache.GetOrAdd(source, static (key, self) => {
            for (var type = key; type != null && type != typeof(object); type = type.BaseType) {
                if (self._map.TryGetValue(type, out var match))
                    return match;
            }
            return null;
        }, this);
}

public static class TypeMapperServiceCollectionExt
{
    public static IServiceCollection AddTypeMapper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TScope>(
        this IServiceCollection services,
        Action<TypeMap<TScope>> typeMapBuilder)
    {
        var typeMap = new TypeMap<TScope>();
        typeMapBuilder.Invoke(typeMap);
        services.AddSingleton(new TypeMapper<TScope>(typeMap));
        return services;
    }
}
