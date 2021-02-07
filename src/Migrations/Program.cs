using System;
using BoardGames.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BoardGames.Migrations
{
    public class Program
    {
        public static void Main(string[] args)
            => CreateHostBuilder(args).Build().Run();

        // EF Core uses this method at design time to access the DbContext
        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder.UseStartup<MigrationsStartup>());
    }

    public class MigrationsStartup
    {
        public string UsePostgreSql =
            "Server=localhost;Database=board_games_dev;Port=5432;User Id=postgres;Password=Fusion.0.to.1";

        public void ConfigureServices(IServiceCollection services)
            => services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    UsePostgreSql,
                    o => o.MigrationsAssembly(typeof(MigrationsStartup).Assembly.FullName)));

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) { }
    }
}
