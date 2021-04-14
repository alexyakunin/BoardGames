using System;
using BoardGames.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Pluralize.NET;
using Stl.Extensibility;
using Stl.Fusion;
using Stl.Fusion.Extensions;

namespace BoardGames.ClientServices
{
    [Module]
    public class ClientServicesModule : AppModuleBase
    {
        public ClientServicesModule(IServiceCollection services, IServiceProvider moduleBuilderServices)
            : base(services, moduleBuilderServices) { }

        public override void Use()
        {
            // Other UI-related services
            Services.AddSingleton<IPluralize, Pluralizer>();
            Services.AddFusion().AddFusionTime();
            base.Use();
        }
    }
}
