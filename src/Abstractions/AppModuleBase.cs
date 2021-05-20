using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Extensibility;

namespace BoardGames.Abstractions
{
    [RegisterModule]
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
            Services.UseRegisterAttributeScanner()
                .RegisterFrom(moduleAssembly) // Add shared services
                .WithScope(HostScope).RegisterFrom(moduleAssembly); // Add host-specific services
        }
    }
}
