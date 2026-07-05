using System.Security.Cryptography.X509Certificates;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Blazor.Authentication;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Npgsql;
using ActualLab.Fusion.EntityFramework.Operations;
using ActualLab.Fusion.Server;
using ActualLab.IO;
using ActualLab.Rpc;
using ActualLab.Rpc.Server;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using BoardGames.Host;
using BoardGames.Host.Components.Pages;
using BoardGames.HostServices;
using BoardGames.Migrations;
using BoardGames.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;
var cfg = builder.Configuration;
var hostSettings = cfg.GetSettings<HostSettings>("BoardGames");

cfg.Sources.Insert(0, new MemoryConfigurationSource() {
    InitialData = new Dictionary<string, string>(StringComparer.Ordinal) {
        { WebHostDefaults.ServerUrlsKey, $"http://localhost:{hostSettings.Port ?? 5030}" },
    }!
});

// Configure services
var services = builder.Services;
ConfigureLogging();
ConfigureServices();
builder.WebHost.UseDefaultServiceProvider((ctx, options) => {
    if (ctx.HostingEnvironment.IsDevelopment()) {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    }
});

// Build & configure app
var app = builder.Build();
StaticLog.Factory = app.Services.LoggerFactory();
var log = StaticLog.For<Program>();
ConfigureApp();

// Migrate the DB to the latest schema (or create it for Sqlite)
{
    var dbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    var database = dbContext.Database;
    if (hostSettings.UseSqlite) {
        await database.EnsureDeletedAsync();
        await database.EnsureCreatedAsync();
    } else {
        await database.MigrateAsync();
    }
}

// Run the app
await app.RunAsync();
return;

// Helpers

void ConfigureLogging()
{
    services.AddLogging(logging => {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
        if (env.IsDevelopment()) {
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
            logging.AddFilter("ActualLab.Fusion.Operations", LogLevel.Information);
        }
    });
}

void ConfigureServices()
{
    services.AddSingleton(hostSettings);

    // DbContext & related services
    var appTempDir = FilePath.GetApplicationTempDirectory("", true);
    var sqliteDbPath = appTempDir & "BoardGames_v1.db";
    services.AddDbContextFactory<AppDbContext>(db => {
        if (hostSettings.UseSqlite)
            db.UseSqlite(
                $"Data Source={sqliteDbPath}",
                o => o.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName));
        else
            db.UseNpgsql(
                hostSettings.UsePostgreSql,
                o => o.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName));
        if (env.IsDevelopment())
            db.EnableSensitiveDataLogging();
    });
    services.AddDbContextServices<AppDbContext>(db => {
        db.AddOperations(operations => {
            operations.ConfigureOperationLogReader(_ => new() {
                // We use FileSystem/Npgsql operation log watchers, so unconditional
                // check period can be arbitrary long - all depends on the reliability
                // of the Notifier-Watcher chain.
                CheckPeriod = TimeSpan.FromSeconds(env.IsDevelopment() ? 60 : 5),
            });
            operations.ConfigureEventLogReader(_ => new() {
                CheckPeriod = TimeSpan.FromSeconds(env.IsDevelopment() ? 60 : 5),
            });
            if (hostSettings.UseSqlite)
                operations.AddFileSystemOperationLogWatcher();
            else
                operations.AddNpgsqlOperationLogWatcher();
        });
        db.AddEntityResolver<string, DbGame>(_ => new() {
            QueryTransformer = games => games.Include(g => g.Players),
        });
    });

    // Fusion services
    var fusion = services.AddFusion(RpcServiceMode.Server, true);
    var fusionServer = fusion.AddWebServer();
    fusion.AddOperationReprocessor();
    fusion.AddDbAuthService<AppDbContext, long>();
    fusionServer.AddAuthEndpoints();
    fusionServer.ConfigureAuthEndpoint(_ => new() {
        DefaultSignInScheme = MicrosoftAccountDefaults.AuthenticationScheme,
        SignInPropertiesBuilder = (_, properties) => {
            properties.IsPersistent = true;
        }
    });
    fusionServer.ConfigureServerAuthHelper(_ => new() {
        NameClaimKeys = [],
    });

    // BoardGames services
    fusion.AddServer<IGameService, GameService>();
    fusion.AddServer<IChatService, ChatService>();
    fusion.AddServer<IAppUserService, AppUserService>();
    fusion.AddSharedServices();

    // Data protection
    var dpCert = X509CertificateLoader.LoadPkcs12(
        Convert.FromBase64String(hostSettings.DataProtectionCert), null);
    services.AddScoped(c => c.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
    // Keys are stored in the shared DB and protected with the certificate,
    // so every replica ends up with the same key ring.
    services.AddDataProtection()
        .SetApplicationName("BoardGames.Host")
        .PersistKeysToDbContext<AppDbContext>()
        .ProtectKeysWithCertificate(dpCert);

    // ASP.NET Core authentication providers
    services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }).AddCookie(options => {
        options.LoginPath = "/signIn";
        options.LogoutPath = "/signOut";
        if (env.IsDevelopment())
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnSigningIn = ctx => {
            ctx.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(28);
            return Task.CompletedTask;
        };
    }).AddMicrosoftAccount(options => {
        options.ClientId = hostSettings.MicrosoftClientId;
        options.ClientSecret = hostSettings.MicrosoftClientSecret;
        // That's for personal account authentication flow
        options.AuthorizationEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    }).AddGitHub(options => {
        options.ClientId = hostSettings.GitHubClientId;
        options.ClientSecret = hostSettings.GitHubClientSecret;
        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    });

    // Web
    services.Configure<ForwardedHeadersOptions>(options => {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        if (!hostSettings.AssumeHttps)
            options.ForwardedHeaders |= ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
    services.AddServerSideBlazor(o => o.DetailedErrors = true);
    services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();
    fusion.AddBlazor().AddAuthentication().AddPresenceReporter(); // Must follow services.AddServerSideBlazor()!
    services.AddBlazorCircuitActivitySuppressor();

    // Shared UI services (Blazorise, update delayer, etc.)
    ClientStartup.ConfigureSharedServices(services);
}

void ConfigureApp()
{
    if (hostSettings.AssumeHttps) {
        log.LogInformation("AssumeHttps on");
        app.Use((context, next) => {
            context.Request.Scheme = "https";
            return next();
        });
    }

    StaticWebAssetsLoader.UseStaticWebAssets(env, cfg);
    if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseWebAssemblyDebugging();
    }
    else {
        app.UseDeveloperExceptionPage();
        app.UseHsts();
    }
    if (hostSettings.UseHttpsRedirection) {
        log.LogInformation("UseHttpsRedirection on");
        app.UseHttpsRedirection();
    }
    if (hostSettings.UseForwardedHeaders) {
        log.LogInformation("UseForwardedHeaders on");
        app.UseForwardedHeaders();
    }

    app.UseWebSockets(new WebSocketOptions() {
        KeepAliveInterval = TimeSpan.FromSeconds(30),
    });
    app.UseFusionSession();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAntiforgery();

    // Razor components
    app.MapStaticAssets();
    app.MapRazorComponents<_HostPage>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(App).Assembly);

    // Fusion endpoints
    app.MapRpcWebSocketServer();
    app.MapFusionAuthEndpoints();
    app.MapFusionRenderModeEndpoints();

    // A tiny API endpoint used to demo the session affinity in multi-host deployments
    app.MapGet("/api/hostInfo/getHostName", () => Environment.MachineName);
}
