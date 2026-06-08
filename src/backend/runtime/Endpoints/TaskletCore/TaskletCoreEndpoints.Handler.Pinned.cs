using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Lists pinned Tasklets owned by the authenticated user.
/// </summary>
/// <remarks>
/// Pinned Tasklets are the important items the UI can always show in a separate
/// lane, so this handler uses the explicit pinned storage entry point.
/// </remarks>
public class PinnedTaskletsHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> PinnedCounter =
        TaskletTelemetry.Metrics.CreateCounter<int>(
            "pinned_tasklet_count",
            description: "The number of pinned Tasklet list requests served."
        );

    /// <summary>
    /// Handles the current user's pinned Tasklet list query.
    /// </summary>
    public async Task<Results<Ok<TaskletListResponse>, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        int skip = 0,
        int take = 25,
        TaskletSortField? sort = null,
        SortDirection direction = SortDirection.Descending
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent([], "tasklet.pinned.unauthorized");

            return TypedResults.Unauthorized();
        }

        var normalizedSkip = Math.Max(skip, 0);
        var normalizedTake = Math.Min(take <= 0 ? 25 : take, 100);
        var tasklets = await storage.GetPinnedTaskletsForUserAsync(
            userId,
            normalizedSkip,
            normalizedTake,
            sort.ToOrderBy(),
            direction
        );
        var taskletResponses = tasklets.Select(tasklet => tasklet.ToResponse()).ToList();
        var response = new TaskletListResponse(taskletResponses, normalizedSkip, normalizedTake);

        PinnedCounter.Add(1);
        TaskletTelemetry.AddEvent(
            [
                ("skip", normalizedSkip),
                ("take", normalizedTake),
                ("sort", sort?.ToString()),
                ("direction", direction.ToString()),
                ("tasklet.count", taskletResponses.Count),
            ],
            "tasklet.pinned.succeeded"
        );

        return TypedResults.Ok(response);
    }
}
