using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BoardGames.Host;
using BoardGames.HostServices;

var host = Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(builder => {
        // Looks like there is no better way to set _default_ URL
        builder.Sources.Insert(0, new MemoryConfigurationSource() {
            InitialData = new Dictionary<string, string>() {
                {WebHostDefaults.ServerUrlsKey, "http://localhost:5030"},
            }
        });
    })
    .ConfigureWebHostDefaults(builder => builder
        .UseDefaultServiceProvider((ctx, options) => {
            if (ctx.HostingEnvironment.IsDevelopment()) {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }
        })
        .UseStartup<Startup>())
    .Build();

// Migrate the DB to the latest schema
var dbContextFactory = host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
await using var dbContext = dbContextFactory.CreateDbContext();
var database = dbContext.Database;
if (database.ProviderName.EndsWith("Sqlite")) {
    await database.EnsureDeletedAsync();
    await database.EnsureCreatedAsync();
} else {
    // await database.EnsureDeletedAsync();
    await database.MigrateAsync();
}

await host.RunAsync();
