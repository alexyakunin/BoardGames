using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Npgsql;
using ActualLab.Fusion.EntityFramework.Operations;
using ActualLab.Fusion.Server;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using BoardGames.HostServices;
using BoardGames.Migrations;
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
    public string ConnectionString { get; private set; } = "";

    public static async Task<TestAppHost> Start()
    {
        var host = new TestAppHost();
        await host.StartServer();
        host.StartClient();
        return host;
    }

    private async Task StartServer()
    {
        // Tests run against PostgreSQL (the docker-compose `db` service). Each test host
        // gets a unique database. Override the base connection string (login/password/host)
        // via BOARDGAMES_TESTS_POSTGRES; only its Database part is replaced.
        var postgresTemplate = Environment.GetEnvironmentVariable("BOARDGAMES_TESTS_POSTGRES");
        if (string.IsNullOrEmpty(postgresTemplate))
            postgresTemplate = "Server=localhost;Database=boardgames;Port=5432;User Id=postgres;Password=postgres";
        ConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(postgresTemplate) {
            Database = $"boardgames_tests_{Ulid.NewUlid().ToString().ToLowerInvariant()}",
        }.ConnectionString;

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(kestrel => kestrel.Listen(System.Net.IPAddress.Loopback, 0));
        var services = builder.Services;

        // DbContext & related services
        services.AddDbContextFactory<AppDbContext>(db => {
            db.UseNpgsql(
                ConnectionString,
                o => o.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName));
        });
        services.AddDbContextServices<AppDbContext>(db => {
            db.AddOperations(operations => {
                operations.AddNpgsqlOperationLogWatcher();
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
            await dbContext.Database.MigrateAsync();

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
                // Drops the unique per-host PostgreSQL database
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
