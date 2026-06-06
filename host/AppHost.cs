var builder = DistributedApplication.CreateBuilder(args);

// The main backend API runtime.
var backend = builder.AddProject<Projects.Tasklet_API>(name: "tasklet-api");

// Standalone watch-build with an environment variable GEN which triggers an OpenAPI schema rebuild for the FE
var buildGenerate = builder
    .AddExecutable(
        "tasklet-schema-publish",
        "dotnet",
        "../src/backend",
        ["watch", "build", "--non-interactive"]
    )
    .WithEnvironment("GEN", "true");

// Vue front-end app.
var frontend = builder.AddViteApp(name: "tasklet-ui", appDirectory: "../src/web").WithYarn();

// Yarp proxy to make it more pleasant to use.
var proxy = builder
    .AddYarp("yarp-reverse-proxy")
    .WithHostPort(8080)
    .WithConfiguration(yarp =>
    {
        // Proxy route to the backend as: http://api.localhost:8089
        yarp.AddRoute(backend).WithMatHosts("api.localhost");

        // Proxy route to the frontend as: http://tasklet.localhost:8089
        yarp.AddRoute(frontend).WithMatHosts("tasklet.localhost");
    });

builder.Build().Run();
