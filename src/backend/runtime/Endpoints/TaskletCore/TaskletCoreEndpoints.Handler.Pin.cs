using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Pins or unpins Tasklets owned by the authenticated user.
/// </summary>
public class PinTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> PinCounter = TaskletTelemetry.Metrics.CreateCounter<int>(
        "pin_tasklet_count",
        description: "The number of Tasklet pin state changes."
    );

    /// <summary>
    /// Marks a Tasklet as pinned for the current user.
    /// </summary>
    public Task<Results<Ok<TaskletResponse>, NotFound, UnauthorizedHttpResult>> Pin(
        ClaimsPrincipal user,
        Guid id
    ) => SetPinned(user, id, pinned: true);

    /// <summary>
    /// Marks a Tasklet as unpinned for the current user.
    /// </summary>
    public Task<Results<Ok<TaskletResponse>, NotFound, UnauthorizedHttpResult>> Unpin(
        ClaimsPrincipal user,
        Guid id
    ) => SetPinned(user, id, pinned: false);

    /// <summary>
    /// Shared handler for user-scoped pin state mutations.
    /// </summary>
    private async Task<Results<Ok<TaskletResponse>, NotFound, UnauthorizedHttpResult>> SetPinned(
        ClaimsPrincipal user,
        Guid id,
        bool pinned
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent(
                [("tasklet.id", id), ("pinned", pinned)],
                "tasklet.pin.unauthorized"
            );

            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.SetTaskletPinnedAsync(id, userId, pinned);

        if (tasklet is null)
        {
            TaskletTelemetry.AddEvent(
                [("tasklet.id", id), ("pinned", pinned)],
                "tasklet.pin.not_found"
            );

            return TypedResults.NotFound();
        }

        PinCounter.Add(1);
        TaskletTelemetry.AddEvent(
            [("tasklet.id", id), ("pinned", pinned)],
            "tasklet.pin.succeeded"
        );

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
