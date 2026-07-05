using BoardGames.HostServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoardGames.Migrations;

// EF Core uses this factory at design time to create migrations,
// e.g.: dotnet ef migrations add <Name> --project src/Migrations
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public string UsePostgreSql =
        "Server=localhost;Database=board_games_dev;Port=5432;User Id=postgres;Password=Fusion.0.to.1";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(UsePostgreSql, o => o.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName))
            .Options;
        return new AppDbContext(options);
    }
}
