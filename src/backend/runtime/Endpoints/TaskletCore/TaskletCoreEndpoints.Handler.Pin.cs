using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Pins or unpins Tasklets owned by the authenticated user.
/// </summary>
public class PinTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
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
            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.SetTaskletPinnedAsync(id, userId, pinned);

        if (tasklet is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
