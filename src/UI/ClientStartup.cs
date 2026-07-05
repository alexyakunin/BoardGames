using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.Client.Caching;
using ActualLab.Fusion.UI;
using ActualLab.Rpc;
using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using BoardGames.ClientServices;
using BoardGames.UI.Game;
using BoardGames.UI.Game.MessageFragmentViews;
using BoardGames.UI.Gomoku;
using BoardGames.UI.RockPaperScissors;
using BoardGames.UI.Services;

namespace BoardGames.UI;

public static class ClientStartup
{
    // Client-side (WASM) services
    public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Default RPC client serialization format
        RpcSerializationFormatResolver.Default = new("msgpack6c");

        // Fusion services
        var fusion = services.AddFusion();
        fusion.AddAuthClient();
        fusion.AddBlazor().AddAuthentication().AddPresenceReporter();

        // RPC clients for the server-side compute services
        fusion.AddClient<IGameService>();
        fusion.AddClient<IChatService>();
        fusion.AddClient<IAppUserService>();
        fusion.Rpc.AddWebSocketClient(builder.HostEnvironment.BaseAddress);

        // LocalStorageRemoteComputedCache as IRemoteComputedCache -
        // it makes the client cache RPC compute method call results
        services.AddBlazoredLocalStorageAsSingleton();
        services.AddSingleton(_ => LocalStorageRemoteComputedCache.Options.Default);
        services.AddSingleton(c => {
            var options = c.GetRequiredService<LocalStorageRemoteComputedCache.Options>();
            return (IRemoteComputedCache)new LocalStorageRemoteComputedCache(options, c);
        });

        ConfigureSharedServices(services);
    }

    // Services shared by the server and the client
    public static void ConfigureSharedServices(IServiceCollection services)
    {
        var fusion = services.AddFusion();
        fusion.AddSharedServices(); // Game engines, message parser, etc.

        // UI-related Fusion services
        services.AddScoped<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.5));

        // Type maps binding models to their views (AOT-friendly - no assembly scanning)
        services.AddTypeMapper<GameRulesBase>(map => map
            .Add<GomokuEngine, GomokuRules>()
            .Add<RpsEngine, RockPaperScissorsRules>());
        services.AddTypeMapper<GamePlayBase>(map => map
            .Add<GomokuEngine, GomokuPlay>()
            .Add<RpsEngine, RockPaperScissorsPlay>());
        services.AddTypeMapper<MessageFragmentView>(map => map
            .Add<PlainText, PlainTextSpan>()
            .Add<UserMention, UserMentionSpan>()
            .Add<GameScoreMention, GameScoreMentionSpan>());

        // Blazorise
        services.AddBlazorise(options => {
                options.Immediate = true;
            })
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
    }
}
