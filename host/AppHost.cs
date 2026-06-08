using Aspire.Hosting.Yarp;

var builder = DistributedApplication.CreateBuilder(args);

// This makes it easier to start without manually creating .env file (which has no secrets in this case)
EnsureFrontendEnvironmentFile(builder.AppHostDirectory);

const string FirebaseEmulatorVolumePath = "/srv/firebase/.data-volume";
const string FirebaseEmulatorDataPath = $"{FirebaseEmulatorVolumePath}/export";
const string FirebaseEmulatorDataVolumeName = "tasklet-firebase-emulator-data";

// The main backend API runtime.
var backend = builder
    .AddProject<Projects.Tasklet_Runtime>(name: "tasklet-api")
    // .NET Firebase Admin SDK requires environment variable for emulator
    // See: https://firebase.google.com/docs/emulator-suite/connect_auth#admin_sdks
    .WithEnvironment("FIREBASE_AUTH_EMULATOR_HOST", "localhost:9099")
    .WithUrlForEndpoint(
        "http",
        url =>
        {
            url.DisplayText = "API Docs";
            url.Url = "http://api.localhost:8089/scalar";
        }
    );

// Standalone watch-build that passes an MSBuild property so schema generation
// works consistently across shells and operating systems.
var buildGenerate = builder
    .AddExecutable(
        "tasklet-schema-publish",
        "dotnet",
        "../src/backend/runtime",
        ["watch", "build", "--non-interactive", "/p:GenerateSchema=true"]
    )
    .WithEnvironment("GEN", "true");

// Add glider MCP server (Codex does not like the CLI app because of sandbox...)
// See: https://glidermcp.com/
var gliderMcp = builder
    .AddExecutable("glider-mcp", "glider", "./", "--transport", "http", "--port", "5055")
    .WithHttpEndpoint(5051, 5055, name: "http", isProxied: true);

// Add Firebase container for auth
var firebase = builder
    .AddDockerfile(
        name: "firebase-emulator",
        contextPath: ".",
        dockerfilePath: "Dockerfile.firebase"
    )
    // Firebase imports from and exports to a subdirectory because exporting
    // directly into the mounted volume root fails with a resource-busy error.
    // A named Docker volume preserves emulator auth state across container rebuilds.
    .WithEnvironment("FIREBASE_EMULATOR_DATA_DIR", FirebaseEmulatorDataPath)
    .WithVolume(FirebaseEmulatorDataVolumeName, FirebaseEmulatorVolumePath)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(9099, 9099, name: "firebase", isProxied: false);

// Vue front-end app.
var frontend = builder
    .AddViteApp(name: "tasklet-ui", appDirectory: "../src/web")
    .WithYarn()
    .WithUrlForEndpoint(
        "http",
        url =>
        {
            url.DisplayText = "Tasklet UI";
            url.Url = "http://tasklet.localhost:8089";
        }
    );

// Yarp proxy to make it more pleasant to use.
var proxy = builder
    .AddYarp("yarp-reverse-proxy")
    .WithHostPort(8089)
    .WithConfiguration(yarp =>
    {
        // Proxy route to the backend as: http://api.localhost:8089
        yarp.AddRoute(backend).WithMatchHosts("api.localhost");

        // Proxy route to the frontend as: http://tasklet.localhost:8089
        yarp.AddRoute(frontend).WithMatchHosts("tasklet.localhost");
    });

builder.Build().Run();

static void EnsureFrontendEnvironmentFile(string appHostDirectory)
{
    var frontendDirectory = Path.GetFullPath(Path.Combine(appHostDirectory, "..", "src", "web"));
    var envPath = Path.Combine(frontendDirectory, ".env");

    if (File.Exists(envPath))
    {
        return;
    }

    var envExamplePath = Path.Combine(frontendDirectory, ".env.example");

    if (!File.Exists(envExamplePath))
    {
        throw new FileNotFoundException(
            "The frontend .env file is missing and no .env.example file exists to seed it.",
            envExamplePath
        );
    }

    // Seed local Vite configuration once so the frontend starts cleanly in a fresh checkout.
    File.Copy(envExamplePath, envPath, overwrite: false);
}
