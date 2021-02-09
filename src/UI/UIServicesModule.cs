using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Pluralize.NET;
using Stl.Extensibility;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Stl.Fusion.Client;

namespace BoardGames.UI
{
    [Module]
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

                Services.AddFusion(fusion => {
                    fusion.AddRestEaseClient(
                        (c, o) => {
                            o.BaseUri = baseUri;
                            o.MessageLogLevel = LogLevel.Information;
                        }).ConfigureHttpClientFactory(
                        (c, name, o) => {
                            var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                            var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                            o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
                        });
                    fusion.AddAuthentication(fusionAuth => {
                        fusionAuth.AddRestEaseClient();
                        fusionAuth.AddBlazor();
                    });
                });
            }

            // UI services: Blazorise, Pluralizer, etc.
            Services.AddBlazorise(options => {
                    options.DelayTextOnKeyPress = true;
                    options.DelayTextOnKeyPressInterval = 300;
                })
                .AddBootstrapProviders()
                .AddFontAwesomeIcons();

            // UI-related Fusion services
            Services.RemoveAll<UpdateDelayer.Options>();
            Services.AddSingleton(c => new UpdateDelayer.Options() {
                Delay = TimeSpan.FromSeconds(0.5),
            });
            Services.RemoveAll<PresenceService.Options>();
            Services.AddSingleton(c => new PresenceService.Options() {
                UpdatePeriod = TimeSpan.FromMinutes(1),
            });

            // Other UI services
            Services.AddSingleton<IMatchingTypeFinder>(new MatchingTypeFinder(typeof(Program).Assembly));

            base.Use();
        }
    }
}
