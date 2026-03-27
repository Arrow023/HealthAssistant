using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using FitnessAgentsWeb.Core.Factories;
using FitnessAgentsWeb.Core.Interfaces;
using FitnessAgentsWeb.Core.Services;
using FitnessAgentsWeb.Core.Configuration;
using FitnessAgentsWeb.Models;
using System.Security.Claims;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to allow long-running SSE connections
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configure request timeout policies (SSE endpoints need longer timeouts)
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy { Timeout = TimeSpan.FromMinutes(5) };
    options.AddPolicy("SSE", new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy { Timeout = TimeSpan.FromMinutes(10) });
});

// Configure Serilog
var logsFolder = Path.Combine(builder.Environment.ContentRootPath, "Logs");
if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);

builder.Host.UseSerilog((context, services, configuration) =>
{
    var otelSection = context.Configuration.GetSection("OpenTelemetry");
    var otelEndpoint = otelSection["Endpoint"];
    var serviceName = otelSection["ServiceName"] ?? "HealthAssistant";
    var apiKey = Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY")
                ?? otelSection["Headers:api-key"];

    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.With(new FitnessAgentsWeb.Core.Logging.TimezoneTimestampEnricher())
        .Enrich.With(new FitnessAgentsWeb.Core.Logging.OtelLogEnricher())
        .WriteTo.Console(outputTemplate: "[{AppTimestamp}] {Level:u3} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            Path.Combine(logsFolder, "fitness-assist-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "[{AppTimestamp}] {Level:u3} {Message:lj}{NewLine}{Exception}");

    if (!string.IsNullOrWhiteSpace(otelEndpoint) && !string.IsNullOrWhiteSpace(apiKey))
    {
        configuration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = otelEndpoint;
            options.Protocol = OtlpProtocol.HttpProtobuf;
            options.IncludedData = IncludedData.MessageTemplateTextAttribute
                                | IncludedData.TraceIdField
                                | IncludedData.SpanIdField
                                | IncludedData.SpecRequiredResourceAttributes;
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName
            };
            options.Headers = new Dictionary<string, string>
            {
                ["api-key"] = apiKey
            };
        });
    }
});

// Add MVC services for upcoming Dashboard
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Conditionally register OIDC when an external provider is configured
var externalAuthSection = builder.Configuration.GetSection("ExternalAuth");
var externalAuthEnabled = externalAuthSection.GetValue<bool>("Enabled");
if (externalAuthEnabled && !string.IsNullOrWhiteSpace(externalAuthSection["Authority"]))
{
    builder.Services.AddAuthentication()
        .AddOpenIdConnect(options =>
        {
            options.Authority = externalAuthSection["Authority"];
            options.ClientId = externalAuthSection["ClientId"];
            options.ClientSecret = externalAuthSection["ClientSecret"];
            options.CallbackPath = externalAuthSection["CallbackPath"] ?? "/signin-oidc";
            options.SignedOutCallbackPath = externalAuthSection["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
            options.SignedOutRedirectUri = "/Auth/Login";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.Scope.Clear();
            var scopes = externalAuthSection.GetSection("Scopes").Get<string[]>() ?? ["openid", "profile", "email"];
            foreach (var scope in scopes)
            {
                options.Scope.Add(scope);
            }

            var nameClaimType = externalAuthSection["NameClaimType"] ?? ClaimTypes.Name;
            options.TokenValidationParameters.NameClaimType = nameClaimType;

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async context =>
                {
                    var identity = context.Principal?.Identity as ClaimsIdentity;
                    if (identity is not null)
                    {
                        // Resolve the user's display name from common OIDC claim types
                        var email = identity.FindFirst(nameClaimType)?.Value
                                 ?? identity.FindFirst(ClaimTypes.Email)?.Value
                                 ?? identity.FindFirst("email")?.Value
                                 ?? identity.FindFirst("preferred_username")?.Value;

                        if (email is not null && !identity.HasClaim(c => c.Type == ClaimTypes.Name))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, email));
                        }

                        // Default SSO users to the "User" role
                        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
                        }
                    }

                    await Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/Auth/Login?error=sso_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
}

// Register Factories
builder.Services.AddSingleton<ConfigurationProviderFactory>();
builder.Services.AddSingleton<IAppConfigurationManager, FirebaseSettingsProvider>();
builder.Services.AddSingleton<IAppConfigurationProvider>(sp => sp.GetRequiredService<IAppConfigurationManager>());
builder.Services.AddSingleton<StorageRepositoryFactory>();

// Expose IStorageRepository directly for Controllers
builder.Services.AddScoped<IStorageRepository>(sp => 
{
    var factory = sp.GetRequiredService<StorageRepositoryFactory>();
    return factory.Create();
});
builder.Services.AddScoped<IHealthDataProcessor>(sp => 
{
    var factory = sp.GetRequiredService<HealthDataProcessorFactory>();
    return factory.Create();
});
builder.Services.AddSingleton<HealthDataProcessorFactory>();
builder.Services.AddSingleton<AiAgentServiceFactory>();
builder.Services.AddSingleton<NotificationServiceFactory>();
builder.Services.AddScoped<INotificationService>(sp => 
{
    var factory = sp.GetRequiredService<NotificationServiceFactory>();
    return factory.Create();
});
builder.Services.AddSingleton<InBodyOcrService>();
builder.Services.AddSingleton<MealVisionService>();
builder.Services.AddSingleton<IAiOrchestratorService, AiOrchestratorService>();
builder.Services.AddScoped<IChatAgentService, ChatAgentService>();

// Vector store and embedding services (optional — gracefully degrade if Qdrant is unreachable)
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IPlanVectorStore, QdrantPlanVectorStore>();

// Async plan generation infrastructure
builder.Services.AddSingleton<IAppNotificationStore, AppNotificationStore>();
builder.Services.AddSingleton<IPlanGenerationTracker, PlanGenerationTracker>();
builder.Services.AddSingleton(Channel.CreateBounded<PlanGenerationJob>(new BoundedChannelOptions(10)
{
    FullMode = BoundedChannelFullMode.Wait
}));
builder.Services.AddHostedService<PlanGenerationBackgroundService>();

// Register Background Service
builder.Services.AddHostedService<WorkoutEmailSchedulerService>();

var app = builder.Build();

// Initialize vector store collection on startup (non-blocking)
try
{
    var vectorStore = app.Services.GetService<IPlanVectorStore>();
    if (vectorStore is not null)
    {
        _ = Task.Run(async () =>
        {
            try { await vectorStore.InitializeAsync(); }
            catch (Exception ex) { app.Logger.LogWarning(ex, "Qdrant vector store initialization failed — AI memory features will be unavailable"); }
        });
    }
}
catch { /* Vector store not configured — gracefully degrade */ }

// Enable routing and static files for the UI Dashboard
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Overview}/{action=Index}/{id?}");

app.Run();