using System;
using BoardGames.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Pluralize.NET;
using Stl.Extensibility;

namespace BoardGames.ClientServices
{
    [Module]
    public class ClientServicesModule : AppModuleBase
    {
        public ClientServicesModule(IServiceCollection services, IServiceProvider moduleBuilderServices)
            : base(services, moduleBuilderServices) { }

        public override void Use()
        {
            // Other UI services
            Services.AddSingleton<IPluralize, Pluralizer>();
            base.Use();
        }
    }
}
