using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using Microsoft.Extensions.Logging;
using Stl.OS;
using Stl.Extensibility;

namespace BoardGames.UI
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var hostBuilder = WebAssemblyHostBuilder.CreateDefault(args);
            hostBuilder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Using modules to register ~ everything
            hostBuilder.Services.UseModules()
                .ConfigureModuleServices(s => {
                    s.AddSingleton(ServiceScope.ClientSideOnly);
                    s.AddSingleton(hostBuilder);
                })
                .Add<AbstractionsModule>()
                .Add<ClientServicesModule>()
                .Add<UIServicesModule>()
                .Use();

            hostBuilder.RootComponents.Add<App>("#app");
            var host = hostBuilder.Build();

            var runTask = host.RunAsync();
            Task.Run(async () => {
                // We "manually" start IHostedServices here, because Blazor host doesn't do this.
                var hostedServices = host.Services.GetRequiredService<IEnumerable<IHostedService>>();
                foreach (var hostedService in hostedServices)
                    await hostedService.StartAsync(default);
            });
            return runTask;
        }
    }
}
