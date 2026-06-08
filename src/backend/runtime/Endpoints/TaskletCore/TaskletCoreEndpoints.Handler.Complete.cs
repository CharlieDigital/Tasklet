using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Completes Tasklets owned by the authenticated user.
/// </summary>
public class CompleteTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> CompleteCounter =
        TaskletTelemetry.Metrics.CreateCounter<int>(
            "complete_tasklet_count",
            description: "The number of Tasklets completed."
        );

    /// <summary>
    /// Marks one Tasklet complete using the server's current UTC timestamp.
    /// </summary>
    public async Task<Results<Ok<TaskletResponse>, NotFound, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        Guid id
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.complete.unauthorized");

            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.CompleteTaskletAsync(id, userId, DateTime.UtcNow);

        if (tasklet is null)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.complete.not_found");

            return TypedResults.NotFound();
        }

        CompleteCounter.Add(1);
        TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.complete.succeeded");

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
