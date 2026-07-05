using BoardGames.Abstractions;
using ActualLab.Fusion.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pluralize.NET;

namespace BoardGames.ClientServices;

public static class SharedServicesExt
{
    /// <summary>
    /// Registers services shared by the server and the client:
    /// game engines, message parser, user name service, etc.
    /// </summary>
    public static FusionBuilder AddSharedServices(this FusionBuilder fusion)
    {
        var services = fusion.Services;
        services.AddGameEngines();
        services.TryAddSingleton<IPluralize, Pluralizer>();
        services.TryAddSingleton<IUserNameService, UserNameService>();
        fusion.AddFusionTime();
        fusion.AddComputeService<IMessageParser, MessageParser>();
        return fusion;
    }
}
