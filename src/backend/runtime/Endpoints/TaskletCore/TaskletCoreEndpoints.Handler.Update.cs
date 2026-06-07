using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Updates a Tasklet owned by the authenticated user.
/// </summary>
public class UpdateTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    /// <summary>
    /// Handles Tasklet updates while preserving ownership and creation time.
    /// </summary>
    public async Task<
        Results<Ok<TaskletResponse>, BadRequest<string>, NotFound, UnauthorizedHttpResult>
    > Handle(ClaimsPrincipal user, Guid id, UpdateTaskletRequest request)
    {
        var userId = user.GetTaskletUserId();

        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var validationError = TaskletRequestValidation.Validate(request);

        if (validationError is not null)
        {
            return TypedResults.BadRequest(validationError);
        }

        var existing = await storage.GetTaskletByIdAsync(id);

        if (existing is null || existing.UserId != userId)
        {
            return TypedResults.NotFound();
        }

        request.ApplyTo(existing);
        await storage.UpdateTaskletAsync(existing);

        return TypedResults.Ok(existing.ToResponse());
    }
}
