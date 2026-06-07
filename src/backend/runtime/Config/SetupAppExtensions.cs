using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using Tasklet.Runtime.Endpoints;

namespace Tasklet.Runtime.Config;

/// <summary>
/// Extension methods for setup.
/// </summary>
public static class SetupAppExtensions
{
    // Extension methods for the WebApplication to set up the HTTP pipeline.
    extension(WebApplication app)
    {
        /// <summary>
        /// Finalizes the HTTP pipeline for Tasklet.
        /// </summary>
        public WebApplication UseTaskletHttp(AppSettings settings, IWebHostEnvironment env)
        {
            // We want to serve the web app from the root URL so we'll serve the backend from /api.
            app.UsePathBase("/api");

            app.UseCors("api-cors-policy");

            app.UseAuthentication();
            app.UseAuthorization();

            // Map the health endpoint directly here.
            app.MapGet(
                    "/health",
                    () => TypedResults.Ok(new HealthResponse("Healthy", DateTimeOffset.UtcNow))
                )
                .AllowAnonymous()
                .WithName("Health")
                .WithTags("Health")
                .WithDescription("Gets the health status of the application.");

            // Map the other endpoints using the IEndpoint interface for organization.
            var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

            // The root group allows us to register global behavior and also
            // routing rules as needed.
            var rootGroup = app.MapGroup("v1").RequireAuthorization();

            foreach (var endpoint in endpoints)
            {
                endpoint.MapEndpoints(rootGroup);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference("/scalar");
            }

            // TODO(production): Test this config by building the container
            if (!app.Environment.IsDevelopment())
            {
                // Upstream when this is deployed, it will be a single container serving the
                // FE from the root URL and the BE from /api, so we need to serve the static
                // files for the FE from the BE in production.
                var appFileProvider = new PhysicalFileProvider(env.WebRootPath);

                // Vue SPA served from root
                app.UseDefaultFiles(
                    new DefaultFilesOptions { RequestPath = "", FileProvider = appFileProvider }
                );

                app.UseStaticFiles(
                    new StaticFileOptions
                    {
                        RequestPath = "",
                        FileProvider = appFileProvider,
                        OnPrepareResponse = ConfigureStaticFileCaching,
                    }
                );

                // Add SPA fallback when served upstream (on local, it's Vite)
                // SPA fallbacks: serve index.html for any unmatched route under each prefix.
                // The :nonfile constraint skips requests that look like static assets (have a file extension)
                // so .js, .css, images, etc. return 404 rather than silently serving index.html.
                app.MapFallbackToFile("/{**path:nonfile}", "index.html");
            }

            return app;
        }
    }

    private static void ConfigureStaticFileCaching(StaticFileResponseContext ctx)
    {
        const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
        string cacheControlValue = $"max-age={durationInSeconds}, immutable";

        var fileExtension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();

        if (fileExtension == ".html")
        {
            // Short cache for HTML files
            cacheControlValue = "max-age=180, private";
        }

        // Add caching headers for static files
        ctx.Context.Response.Headers.Append("Cache-Control", cacheControlValue);
    }
}

/// <summary>
/// Simple healthcheck response
/// </summary>
internal record HealthResponse(string Status, DateTimeOffset CheckedAt);
