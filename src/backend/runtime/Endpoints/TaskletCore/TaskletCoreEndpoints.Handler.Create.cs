using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Creates a Tasklet for the authenticated user.
/// </summary>
public class CreateTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    /// <summary>
    /// Handles Tasklet creation and assigns ownership from the authenticated user.
    /// </summary>
    public async Task<
        Results<Created<TaskletResponse>, BadRequest<string>, UnauthorizedHttpResult>
    > Handle(ClaimsPrincipal user, CreateTaskletRequest request)
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

        var created = await storage.CreateTaskletAsync(request.ToTasklet(userId));

        return TypedResults.Created($"/v1/tasklets/{created.Id}", created.ToResponse());
    }
}
