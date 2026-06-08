using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Lists Tasklets owned by the authenticated user.
/// </summary>
public class ListTaskletsHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> ListCounter = TaskletTelemetry.Metrics.CreateCounter<int>(
        "list_tasklet_count",
        description: "The number of Tasklet list requests served."
    );

    /// <summary>
    /// Handles the current user's Tasklet list query.
    /// </summary>
    public async Task<Results<Ok<TaskletListResponse>, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        int skip = 0,
        int take = 25,
        TaskletSortField? sort = null,
        SortDirection direction = SortDirection.Descending,
        string? filter = null
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            TaskletTelemetry.AddEvent([], "tasklet.list.unauthorized");

            return TypedResults.Unauthorized();
        }

        var normalizedSkip = Math.Max(skip, 0);
        var normalizedTake = Math.Min(take <= 0 ? 25 : take, 100);
        var tasklets = await storage.GetTaskletsForUserAsync(
            userId,
            normalizedSkip,
            normalizedTake,
            sort.ToOrderBy(),
            direction,
            filter
        );
        var taskletResponses = tasklets.Select(tasklet => tasklet.ToResponse()).ToList();
        var response = new TaskletListResponse(taskletResponses, normalizedSkip, normalizedTake);

        ListCounter.Add(1);
        TaskletTelemetry.AddEvent(
            [
                ("skip", normalizedSkip),
                ("take", normalizedTake),
                ("sort", sort?.ToString()),
                ("direction", direction.ToString()),
                ("has_filter", !string.IsNullOrWhiteSpace(filter)),
                ("tasklet.count", taskletResponses.Count),
            ],
            "tasklet.list.succeeded"
        );

        return TypedResults.Ok(response);
    }
}
