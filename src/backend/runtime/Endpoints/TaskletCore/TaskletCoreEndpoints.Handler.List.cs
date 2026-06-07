using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Lists Tasklets owned by the authenticated user.
/// </summary>
public class ListTaskletsHandler(ITaskletStorage storage) : IEndpointHandler
{
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
        var response = new TaskletListResponse(
            tasklets.Select(tasklet => tasklet.ToResponse()).ToList(),
            normalizedSkip,
            normalizedTake
        );

        return TypedResults.Ok(response);
    }
}
