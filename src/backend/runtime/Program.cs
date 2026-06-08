using Tasklet.Core;
using Tasklet.Core.Model;
using Tasklet.Runtime.Config;

Console.WriteLine("Starting Tasklet Runtime...");

var builder = WebApplication.CreateBuilder(args);

// Set up logging first; injected Serilog as ILogger so we get OTEL structured logs
builder.AddTaskletLogging();

// Add Scalar API services for testing
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Servers = [new() { Url = "http://api.localhost:8089/api" }];
            return Task.CompletedTask;
        }
    );
});

// Load the configuration.
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

var settings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

if (settings == null)
{
    Environment.Exit(1); // ! EXIT: Couldn't load the config.
}

// ⭐️ Add core services for Tasklet.
builder
    .Services.AddFirebaseAuthentication(settings, builder.Environment)
    .AddTaskletHttp(settings)
    .AddTaskletStorage(settings);

var app = builder.Build();

app.Logger.LogInformation("Initializing Tasklet storage.");

await using (var scope = app.Services.CreateAsyncScope())
{
    // Ru n storage initialization logic, which applies any pending migrations,
    // index setup, etc.  Storage implementation is responsible for idempotency.
    var storage = scope.ServiceProvider.GetRequiredService<ITaskletStorage>();
    await storage.InitializeAsync();
}

app.Logger.LogInformation("Tasklet storage initialized.");

app.UseTaskletHttp(settings, builder.Environment);

// ⭐️ Application starts here.
app.Run();
