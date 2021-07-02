using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.OS;
using Stl.Extensibility;

namespace BoardGames.UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var hostBuilder = WebAssemblyHostBuilder.CreateDefault(args);
            hostBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
            // Using modules to register everything
            hostBuilder.Services.UseModules()
                .ConfigureModuleServices(s => {
                    s.AddSingleton(ServiceScope.ClientSideOnly);
                    s.AddSingleton(hostBuilder);
                })
                .Add<AbstractionsModule>()
                .Add<ClientServicesModule>()
                .Add<UIServicesModule>()
                .Use();

            var host = hostBuilder.Build();
            await host.Services.HostedServices().Start();
            await host.RunAsync();
        }
    }
}
