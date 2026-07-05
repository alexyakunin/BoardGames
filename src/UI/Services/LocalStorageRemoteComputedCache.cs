using ActualLab.Fusion.Client.Caching;
using ActualLab.IO;
using ActualLab.Rpc.Caching;
using Blazored.LocalStorage;
using MessagePack;

namespace BoardGames.UI.Services;

/// <summary>
/// Client-side (local storage based) cache for RPC compute method call results.
/// It allows the WASM client to instantly render the UI from cached data on startup,
/// and update it a bit later, once the RPC call results arrive.
/// </summary>
public sealed class LocalStorageRemoteComputedCache : RemoteComputedCache
{
    public new record Options : RemoteComputedCache.Options
    {
        public static new readonly Options Default = new() { Version = "1.0" };

        public string KeyPrefix { get; init; } = "";
    }

    [ThreadStatic] private static ArrayPoolBuffer<byte>? _writeBuffer;
    private readonly ISyncLocalStorageService _storage;
    private readonly string _keyPrefix;

    // ReSharper disable once ConvertToPrimaryConstructor
    public LocalStorageRemoteComputedCache(Options settings, IServiceProvider services, bool initialize = true)
        : base(settings, services, initialize)
    {
        _keyPrefix = settings.KeyPrefix;
        _storage = services.GetRequiredService<ISyncLocalStorageService>();
    }

    public override ValueTask<RpcCacheValue?> Get(RpcCacheKey key, CancellationToken cancellationToken = default)
    {
        var sValue = _storage.GetItemAsString(GetStringKey(key));
        if (sValue.IsNullOrEmpty())
            return default;

        var bytes = Convert.FromBase64String(sValue);
        var value = MessagePackSerializer.Deserialize<RpcCacheValue>(
            bytes, MessagePackByteSerializer.DefaultOptions);
        return new ValueTask<RpcCacheValue?>(value);
    }

    public override void Set(RpcCacheKey key, RpcCacheValue value)
    {
        var buffer = ArrayPoolBuffer<byte>.NewOrRenew(ref _writeBuffer, 4096, 65536, false);
        MessagePackSerializer.Serialize(buffer, value, MessagePackByteSerializer.DefaultOptions);
        var sValue = Convert.ToBase64String(buffer.WrittenSpan);
        _storage.SetItemAsString(GetStringKey(key), sValue);
    }

    public override void Remove(RpcCacheKey key)
        => _storage.RemoveItem(GetStringKey(key));

    public override Task Clear(CancellationToken cancellationToken = default)
    {
        _storage.Clear();
        return Task.CompletedTask;
    }

    private string GetStringKey(RpcCacheKey key)
    {
        var data = Convert.ToBase64String(key.ArgumentData.Span);
        return string.Concat(_keyPrefix, key.Name, " ", data);
    }
}
