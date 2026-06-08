using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Deletes a Tasklet owned by the authenticated user.
/// </summary>
public class DeleteTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> DeleteCounter =
        TaskletTelemetry.Metrics.CreateCounter<int>(
            "delete_tasklet_count",
            description: "The number of Tasklets deleted."
        );

    /// <summary>
    /// Handles Tasklet deletion while enforcing ownership.
    /// </summary>
    public async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        Guid id
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.delete.unauthorized");

            return TypedResults.Unauthorized();
        }

        var existing = await storage.GetTaskletByIdAsync(id);

        if (existing is null || existing.UserId != userId)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.delete.not_found");

            return TypedResults.NotFound();
        }

        await storage.DeleteTaskletAsync(id);

        DeleteCounter.Add(1);
        TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.delete.succeeded");

        return TypedResults.NoContent();
    }
}
