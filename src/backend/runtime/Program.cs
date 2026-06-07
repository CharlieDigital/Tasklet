using Tasklet.Core;
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
builder.Services.AddFirebaseAuthentication(settings).AddTaskletHttp(settings);

var app = builder.Build();

app.UseTaskletHttp(settings, builder.Environment);

// ⭐️ Application starts here.
app.Run();
