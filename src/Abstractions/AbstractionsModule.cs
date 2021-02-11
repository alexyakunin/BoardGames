using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Stl.Extensibility;

namespace BoardGames.Abstractions
{
    [Module]
    public class AbstractionsModule : AppModuleBase
    {
        public AbstractionsModule(IServiceCollection services, IServiceProvider moduleBuilderServices)
            : base(services, moduleBuilderServices) { }

        public override void Use()
        {
            // Game engines dictionary
            Services.AddSingleton(c =>
                c.GetRequiredService<IEnumerable<IGameEngine>>().ToImmutableDictionary(e => e.Id));

            base.Use();
        }
    }
}
