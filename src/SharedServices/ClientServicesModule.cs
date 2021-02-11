using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            // Game engines
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, GomokuEngine>());
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IGameEngine, DiceEngine>());
            Services.AddSingleton(c =>
                c.GetRequiredService<IEnumerable<IGameEngine>>().ToImmutableDictionary(e => e.Id));

            // Other UI services
            Services.AddSingleton<IPluralize, Pluralizer>();
            base.Use();
        }
    }
}
