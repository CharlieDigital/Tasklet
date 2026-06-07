using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Completes Tasklets owned by the authenticated user.
/// </summary>
public class CompleteTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
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
            return TypedResults.Unauthorized();
        }

        var tasklet = await storage.CompleteTaskletAsync(id, userId, DateTime.UtcNow);

        if (tasklet is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(tasklet.ToResponse());
    }
}
