using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Deletes a Tasklet owned by the authenticated user.
/// </summary>
public class DeleteTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    /// <summary>
    /// Handles Tasklet deletion while enforcing ownership.
    /// </summary>
    public async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user,
        Guid id
    )
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var existing = await storage.GetTaskletByIdAsync(id);

        if (existing is null || existing.UserId != userId)
        {
            return TypedResults.NotFound();
        }

        await storage.DeleteTaskletAsync(id);

        return TypedResults.NoContent();
    }
}
