using System.Text.Json.Serialization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Tasklet.Core;
using Tasklet.Core.Endpoints;
using Tasklet.Runtime.Middleware;

namespace Tasklet.Runtime.Config;

/// <summary>
/// Extension methods for setup.
/// </summary>
public static class SetupServicesExtensions
{
    private static readonly Dictionary<string, object> DefaultAttributes = new()
    {
        ["service"] = "tasklet",
        ["service.name"] = "tasklet",
    };

    // Extension methods for the IServiceCollection to set up services.
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Sets up Firebase Admin SDK for authentication.
        /// </summary>
        public IServiceCollection AddFirebaseAuthentication(AppSettings settings)
        {
            FirebaseApp.Create(
                new AppOptions()
                {
                    Credential = GoogleCredential.GetApplicationDefault(),
                    ProjectId = settings.Firebase?.ProjectId ?? "missing-firebase-project-id",
                }
            );

            services
                .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
                .AddScheme<FirebaseAuthenticationOptions, FirebaseAuthenticationHandler>(
                    FirebaseAuthenticationHandler.SchemeName,
                    options => { }
                );

            return services;
        }

        /// <summary>
        /// Sets up HTTP pipeline for Tasklet
        /// </summary>
        public IServiceCollection AddTaskletHttp(AppSettings settings)
        {
            // CORS config consumed later...
            services.AddCors(options =>
                options.AddPolicy(
                    "api-cors-policy",
                    policy =>
                        policy
                            .WithOrigins([.. settings.Auth?.AllowedCorsOrigins ?? []])
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                )
            );

            services.AddAuthorization();

            // Configure JSON options for consistent serialization across the app.
            services.ConfigureHttpJsonOptions(json =>
            {
                json.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                json.SerializerOptions.PropertyNameCaseInsensitive = true;
                json.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            });

            // Just in case we need it.
            services.AddHttpContextAccessor();

            // Register all IEndpoint implementations for automatic endpoint mapping.
            services.Scan(scan =>
                scan.FromAssemblyOf<Program>()
                    // Register all IEndpoint instances
                    .AddClasses(classes => classes.AssignableTo<IEndpoint>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                    // Now register all of the handlers
                    // Handlers are injected by concrete type via [FromServices], so
                    // they must be registered as self rather than by interface.
                    .AddClasses(classes => classes.AssignableTo<IEndpointHandler>())
                    .AsSelf()
                    .WithTransientLifetime()
            );

            return services;
        }

        /// <summary>
        /// Sets up telemetry for Tasklet.
        /// </summary>
        public IServiceCollection AddTaskletTelemetry()
        {
            // TODO(production): Add more instrumentation for EF, HTTP, etc. (too noisy right now)
            services
                .AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("tasklet").AddAttributes(DefaultAttributes))
                .WithTracing(b =>
                {
                    b.AddSource(
                            "Tasklet.*",
                            "System.Net.Http",
                            "Private.InternalDiagnostics.System.Net.Http"
                        )
                        .AddAspNetCoreInstrumentation(config =>
                        {
                            config.RecordException = true;
                        })
                        .ConfigureResource(r =>
                            r.AddService("tasklet:http").AddAttributes(DefaultAttributes)
                        );
                })
                .WithMetrics(b => b.AddMeter("*").AddAspNetCoreInstrumentation())
                .WithLogging()
                .UseOtlpExporter();

            return services;
        }
    }

    // Setup for logging; requires the builder.
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Sets up logging for Tasklet.  We use Serilog to OTEL
        /// </summary>
        public void AddTaskletLogging()
        {
            var logConfiguration = new LoggerConfiguration();

            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine(
                    $"Using development logging template: {LoggingConstants.DevelopmentTemplate}"
                );

                logConfiguration.WriteTo.Console(
                    outputTemplate: LoggingConstants.DevelopmentTemplate
                );
            }
            else
            {
                logConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }

            // Structured logging to OTEL (see in Aspire)
            logConfiguration
                .WriteTo.OpenTelemetry(options =>
                {
                    options.ResourceAttributes = SetupServicesExtensions.DefaultAttributes;
                })
                .MinimumLevel.Debug();

            logConfiguration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
            // 👆 Comment out to see auth errors

            Log.Logger = logConfiguration.CreateLogger();

            builder.Logging.ClearProviders();
            builder.Services.AddSerilog(Log.Logger);
            builder.Host.UseSerilog();
            builder.Services.AddSingleton(Log.Logger);
        }
    }
}

/// <summary>
/// Static class for wrapping logging constants
/// </summary>
internal static class LoggingConstants
{
    /// <summary>
    /// The expression statement for development logging.
    /// </summary>
    internal static readonly string DevelopmentTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} ({Here}){NewLine}{Exception}";

    /// <summary>
    /// The expression statement for production logging.
    /// </summary>
    internal static readonly string ProductionTemplate =
        "[{Level:u3}] {Message:lj} ({Here}){NewLine}{Exception}";
}
