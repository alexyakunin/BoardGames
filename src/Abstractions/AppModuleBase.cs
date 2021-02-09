using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Extensibility;

namespace BoardGames.Abstractions
{
    [Module]
    public abstract class AppModuleBase : ModuleBase
    {
        public IServiceProvider ModuleBuilderServices { get; }
        public string HostScope { get; }

        public AppModuleBase(IServiceCollection services, IServiceProvider moduleBuilderServices) : base(services)
        {
            ModuleBuilderServices = moduleBuilderServices;
            HostScope = moduleBuilderServices.GetRequiredService<string>();
        }

        public override void Use()
        {
            var moduleAssembly = GetType().Assembly;
            Services.UseAttributeScanner()
                .AddServicesFrom(moduleAssembly) // Add shared services
                .WithScope(HostScope).AddServicesFrom(moduleAssembly); // Add host-specific services
        }
    }
}
