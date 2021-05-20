using System;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using BoardGames.Abstractions;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Stl.Extensibility;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Stl.Fusion.Client;

namespace BoardGames.UI
{
    [RegisterModule]
    public class UIServicesModule : AppModuleBase
    {
        public WebAssemblyHostBuilder? WebAssemblyHostBuilder { get; }

        public UIServicesModule(IServiceCollection services, IServiceProvider moduleBuilderServices)
            : base(services, moduleBuilderServices)
            => WebAssemblyHostBuilder = ModuleBuilderServices.GetService<WebAssemblyHostBuilder>();

        public override void Use()
        {
            if (WebAssemblyHostBuilder != null) {
                // WASM-only services
                var baseUri = new Uri(WebAssemblyHostBuilder.HostEnvironment.BaseAddress);
                var apiBaseUri = new Uri($"{baseUri}api/");

                var fusion = Services.AddFusion();
                var fusionClient = fusion.AddRestEaseClient((_, o) => o.BaseUri = baseUri);
                fusionClient.ConfigureHttpClientFactory((c, name, o) => {
                    var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                    var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                    o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
                });
                fusion.AddAuthentication(fusionAuth => {
                    fusionAuth.AddRestEaseClient();
                    fusionAuth.AddBlazor();
                });
            }

            // UI services: Blazorise, etc.
            Services.AddBlazorise().AddBootstrapProviders().AddFontAwesomeIcons();

            // UI-related Fusion services
            Services.RemoveAll<IUpdateDelayer>().AddSingleton<IUpdateDelayer>(_ => new UpdateDelayer(0.5));
            Services.RemoveAll<PresenceService.Options>().AddSingleton(
                _ => new PresenceService.Options() { UpdatePeriod = TimeSpan.FromMinutes(1) });

            // Other UI services
            Services.AddSingleton<IMatchingTypeFinder>(new MatchingTypeFinder(typeof(Program).Assembly));

            base.Use();
        }
    }
}
