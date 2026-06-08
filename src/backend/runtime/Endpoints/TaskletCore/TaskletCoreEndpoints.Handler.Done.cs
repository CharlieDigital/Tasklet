using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Lists completed Tasklets owned by the authenticated user.
/// </summary>
public class DoneTaskletsHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> DoneCounter = TaskletTelemetry.Metrics.CreateCounter<int>(
        "done_tasklet_count",
        description: "The number of completed Tasklet list requests served."
    );

    /// <summary>
    /// Handles the current user's completed Tasklet list query.
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
            TaskletTelemetry.AddEvent([], "tasklet.done.unauthorized");

            return TypedResults.Unauthorized();
        }

        var normalizedSkip = Math.Max(skip, 0);
        var normalizedTake = Math.Min(take <= 0 ? 25 : take, 100);
        var tasklets = await storage.GetDoneTaskletsForUserAsync(
            userId,
            normalizedSkip,
            normalizedTake,
            sort.ToOrderBy(),
            direction
        );
        var taskletResponses = tasklets.Select(tasklet => tasklet.ToResponse()).ToList();
        var response = new TaskletListResponse(taskletResponses, normalizedSkip, normalizedTake);

        DoneCounter.Add(1);
        TaskletTelemetry.AddEvent(
            [
                ("skip", normalizedSkip),
                ("take", normalizedTake),
                ("sort", sort?.ToString()),
                ("direction", direction.ToString()),
                ("tasklet.count", taskletResponses.Count),
            ],
            "tasklet.done.succeeded"
        );

        return TypedResults.Ok(response);
    }
}
