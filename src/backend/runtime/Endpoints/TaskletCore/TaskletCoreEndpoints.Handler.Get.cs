using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Gets one Tasklet owned by the authenticated user.
/// </summary>
public class GetTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
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
            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.GetTaskletByIdAsync(id);

        if (tasklet is null || tasklet.UserId != userId)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
