using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Gets one Tasklet owned by the authenticated user.
/// </summary>
public class GetTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> GetCounter = TaskletTelemetry.Metrics.CreateCounter<int>(
        "get_tasklet_count",
        description: "The number of Tasklets retrieved by id."
    );

    /// <summary>
    /// Handles one Tasklet lookup while enforcing ownership.
    /// </summary>
    public async Task<Results<Ok<TaskletResponse>, NotFound, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        Guid id
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.get.unauthorized");

            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.GetTaskletByIdAsync(id);

        if (tasklet is null || tasklet.UserId != userId)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.get.not_found");

            return TypedResults.NotFound();
        }

        GetCounter.Add(1);
        TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.get.succeeded");

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
