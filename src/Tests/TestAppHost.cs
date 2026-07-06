using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Npgsql;
using ActualLab.Fusion.EntityFramework.Operations;
using ActualLab.Fusion.Server;
using ActualLab.IO;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using BoardGames.HostServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

namespace BoardGames.Tests;

/// <summary>
/// Spins up a real (Kestrel-based) BoardGames server host and a Fusion RPC client
/// connected to it over a WebSocket - similarly to how such tests are implemented
/// in the Fusion and ActualChat repositories.
/// </summary>
public sealed class TestAppHost : IAsyncDisposable
{
    private long _lastUserId;

    public WebApplication App { get; private set; } = null!;
    public IServiceProvider ServerServices => App.Services;
    public IServiceProvider ClientServices { get; private set; } = null!;
    public string BaseUrl { get; private set; } = "";
    public FilePath DbPath { get; private set; }

    public static async Task<TestAppHost> Start()
    {
        var host = new TestAppHost();
        await host.StartServer();
        host.StartClient();
        return host;
    }

    private async Task StartServer()
    {
        // Sqlite is used by default; set BOARDGAMES_TESTS_POSTGRES to a Npgsql
        // connection string (its Database part is replaced with a unique name)
        // to run the tests against PostgreSQL instead.
        var postgresTemplate = Environment.GetEnvironmentVariable("BOARDGAMES_TESTS_POSTGRES");
        var usePostgres = !string.IsNullOrEmpty(postgresTemplate);
        var postgresConnectionString = "";
        if (usePostgres) {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(postgresTemplate) {
                Database = $"board_games_tests_{Ulid.NewUlid().ToString().ToLowerInvariant()}",
            };
            postgresConnectionString = csb.ConnectionString;
        }
        else {
            var appTempDir = FilePath.GetApplicationTempDirectory("", true);
            DbPath = appTempDir & $"BoardGames_Tests_{Ulid.NewUlid()}.db";
        }

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(kestrel => kestrel.Listen(System.Net.IPAddress.Loopback, 0));
        var services = builder.Services;

        // DbContext & related services
        services.AddDbContextFactory<AppDbContext>(db => {
            if (usePostgres)
                db.UseNpgsql(postgresConnectionString);
            else
                db.UseSqlite($"Data Source={DbPath}");
        });
        services.AddDbContextServices<AppDbContext>(db => {
            db.AddOperations(operations => {
                if (usePostgres)
                    operations.AddNpgsqlOperationLogWatcher();
                else
                    operations.AddFileSystemOperationLogWatcher();
            });
            db.AddEntityResolver<string, DbGame>(_ => new() {
                QueryTransformer = games => games.Include(g => g.Players),
            });
        });

        // Fusion services
        var fusion = services.AddFusion(RpcServiceMode.Server, true);
        fusion.AddWebServer();
        fusion.AddOperationReprocessor();
        fusion.AddDbAuthService<AppDbContext, long>();
        fusion.AddServer<IGameService, GameService>();
        fusion.AddServer<IChatService, ChatService>();
        fusion.AddServer<IAppUserService, AppUserService>();
        fusion.AddSharedServices();

        App = builder.Build();
        App.UseWebSockets();
        App.MapRpcWebSocketServer();

        var dbContextFactory = App.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
            await dbContext.Database.EnsureCreatedAsync();

        await App.StartAsync();
        var server = App.Services.GetRequiredService<IServer>();
        BaseUrl = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
    }

    private void StartClient()
    {
        // The same RPC serialization format the WASM client uses
        RpcSerializationFormatResolver.Default = new("msgpack6c");

        var services = new ServiceCollection();
        services.AddLogging();
        var fusion = services.AddFusion();
        fusion.AddAuthClient();
        fusion.AddClient<IGameService>();
        fusion.AddClient<IChatService>();
        fusion.AddClient<IAppUserService>();
        fusion.Rpc.AddWebSocketClient(BaseUrl);
        fusion.AddSharedServices();
        ClientServices = services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a new session and signs in a new user with the given name - server-side,
    /// the same way the sign-in flow does it.
    /// </summary>
    public async Task<Session> SignIn(string name)
    {
        var session = Session.New();
        var userId = Interlocked.Increment(ref _lastUserId);
        var user = new User("", name).WithIdentity(new UserIdentity("test", $"test-{userId}-{name}"));
        var command = new AuthBackend_SignIn(session, user);
        await ServerServices.Commander().Call(command);
        return session;
    }

    public async ValueTask DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable cs)
            await cs.DisposeAsync();
        if (App != null!) {
            try {
                // Works for both Sqlite (deletes the file) and PostgreSQL (drops the DB)
                var dbContextFactory = App.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                    await dbContext.Database.EnsureDeletedAsync();
            }
            catch {
                // Intentional
            }
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}
