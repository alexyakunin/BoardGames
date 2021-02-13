using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using BoardGames.HostServices;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Blazor;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using BoardGames.Abstractions;
using BoardGames.ClientServices;
using BoardGames.Migrations;
using BoardGames.UI;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Stl.Extensibility;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.Operations.Internal;
using Stl.IO;

namespace BoardGames.Host
{
    public class Startup
    {
        private IConfiguration Cfg { get; }
        private IWebHostEnvironment Env { get; }
        private HostSettings HostSettings { get; set; } = null!;
        private ILogger Log { get; set; } = NullLogger<Startup>.Instance;

        public Startup(IConfiguration cfg, IWebHostEnvironment environment)
        {
            Cfg = cfg;
            Env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(logging => {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                if (Env.IsDevelopment()) {
                    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
                    logging.AddFilter("Stl.Fusion.Operations", LogLevel.Information);
                }
            });

            // Creating Log and HostSettings as early as possible
#pragma warning disable ASP0000
            var tmpServices = services
                .UseAttributeScanner(s => s.AddService<HostSettings>())
                .BuildServiceProvider();
#pragma warning restore ASP0000
            Log = tmpServices.GetRequiredService<ILogger<Startup>>();
            HostSettings = tmpServices.GetRequiredService<HostSettings>();

            // DbContext & related services
            var appTempDir = PathEx.GetApplicationTempDirectory("", true);
            var sqliteDbPath = appTempDir & "App_v0_1.db";
            services.AddDbContextFactory<AppDbContext>(builder => {
                if (HostSettings.UseSqlite) {
                    Log.LogInformation("DB: Sqlite @ {sqliteDbPath}", sqliteDbPath);
                    builder.UseSqlite(
                        $"Data Source={sqliteDbPath}",
                        o => o.MigrationsAssembly(typeof(MigrationsStartup).Assembly.FullName));
                }
                else {
                    Log.LogInformation("DB: PostgreSql");
                    builder.UseNpgsql(
                        HostSettings.UsePostgreSql,
                        o => o.MigrationsAssembly(typeof(MigrationsStartup).Assembly.FullName));
                }
                if (Env.IsDevelopment())
                    builder.EnableSensitiveDataLogging();
            });
            services.AddDbContextServices<AppDbContext>(b => {
                services.AddSingleton(new CompletionProducer.Options() {
                    LogLevel = LogLevel.Information, // Let's log completions of "external" operations
                });
                b.AddDbOperations((_, o) => {
                    // We use FileBasedDbOperationLogChangeMonitor, so unconditional wake up period
                    // can be arbitrary long - all depends on the reliability of Notifier-Monitor chain.
                    o.UnconditionalWakeUpPeriod = TimeSpan.FromSeconds(Env.IsDevelopment() ? 60 : 5);
                });
                b.AddKeyValueStore();
                var operationLogChangeAlertPath = sqliteDbPath + "_changed";
                b.AddFileBasedDbOperationLogChangeNotifier(operationLogChangeAlertPath);
                b.AddFileBasedDbOperationLogChangeMonitor(operationLogChangeAlertPath);
                b.AddDbAuthentication((_, options) => {
                    options.MinUpdatePresencePeriod = TimeSpan.FromSeconds(55);
                });
                b.AddDbEntityResolver<string, DbGame>((_, options) => {
                    options.QueryTransformer = games => games.Include(g => g.Players);
                });
            });

            // Fusion services
            services.AddSingleton(new Publisher.Options() { Id = HostSettings.PublisherId });
            var fusion = services.AddFusion();
            var fusionServer = fusion.AddWebServer();
            var fusionClient = fusion.AddRestEaseClient();
            var fusionAuth = fusion.AddAuthentication().AddServer(
                signInControllerOptionsBuilder: (_, options) => {
                    options.DefaultScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                });

            // Data protection
            services.AddScoped(c => c.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
            services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

            // Authentication
            services.AddAuthentication(options => {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(options => {
                options.LoginPath = "/signIn";
                options.LogoutPath = "/signOut";
                if (Env.IsDevelopment())
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            }).AddMicrosoftAccount(options => {
                options.ClientId = HostSettings.MicrosoftClientId;
                options.ClientSecret = HostSettings.MicrosoftClientSecret;
                // That's for personal account authentication flow
                options.AuthorizationEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
                options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            }).AddGitHub(options => {
                options.ClientId = HostSettings.GitHubClientId;
                options.ClientSecret = HostSettings.GitHubClientSecret;
                options.Scope.Add("read:user");
                options.Scope.Add("user:email");
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            });

            // Web
            services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            services.AddRouting();
            services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly());
            services.AddServerSideBlazor(o => o.DetailedErrors = true);
            fusionAuth.AddBlazor(o => { }); // Must follow services.AddServerSideBlazor()!

            // Swagger & debug tools
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "BoardGames API", Version = "v1"
                });
            });

            // Using modules to register ~ everything else
            services.UseModules()
                .ConfigureModuleServices(s => {
                    s.AddSingleton(ServiceScope.ServerSideOnly);
                    s.AddSingleton(Log);
                    s.AddSingleton(HostSettings);
                })
                .Add<AbstractionsModule>()
                .Add<ClientServicesModule>()
                .Add<HostServicesModule>()
                .Add<UIServicesModule>()
                .Use();
        }

        public void Configure(IApplicationBuilder app, ILogger<Startup> log)
        {
            if (HostSettings.AssumeHttps) {
                Log.LogInformation("AssumeHttps on");
                app.Use((context, next) => {
                    context.Request.Scheme = "https";
                    return next();
                });
            }

            // This server serves static content from Blazor Client,
            // and since we don't copy it to local wwwroot,
            // we need to find Client's wwwroot in bin/(Debug/Release) folder
            // and set it as this server's content root.
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var binCfgPart = Regex.Match(baseDir, @"[\\/]bin[\\/]\w+[\\/]").Value;
            var wwwRootPath = Path.Combine(baseDir, "wwwroot");
            if (!Directory.Exists(Path.Combine(wwwRootPath, "_framework")))
                // This is a regular build, not a build produced w/ "publish",
                // so we remap wwwroot to the client's wwwroot folder
                wwwRootPath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../UI/{binCfgPart}/net5.0/wwwroot"));
            Env.WebRootPath = wwwRootPath;
            Env.WebRootFileProvider = new PhysicalFileProvider(Env.WebRootPath);
            StaticWebAssetsLoader.UseStaticWebAssets(Env, Cfg);

            if (Env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else {
                // app.UseExceptionHandler("/Error");
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }
            if (HostSettings.UseHttpsRedirection) {
                Log.LogInformation("UseHttpsRedirection on");
                app.UseHttpsRedirection();
            }
            if (HostSettings.UseForwardedHeaders) {
                Log.LogInformation("UseForwardedHeaders on");
                app.UseForwardedHeaders();
            }

            app.UseWebSockets(new WebSocketOptions() {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
            });
            app.UseFusionSession();

            // Static + Swagger
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            });

            // API controllers
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.ApplicationServices.UseBootstrapProviders().UseFontAwesomeIcons(); // Blazorise
            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFusionWebSocketServer();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
