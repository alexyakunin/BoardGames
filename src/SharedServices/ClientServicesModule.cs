using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pluralize.NET;
using Stl.Extensibility;
using Stl.Fusion;
using Stl.Fusion.Extensions;

namespace BoardGames.ClientServices
{
    [RegisterModule]
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
