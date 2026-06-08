using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Creates a Tasklet for the authenticated user.
/// </summary>
public class CreateTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> CreateCounter =
        TaskletTelemetry.Metrics.CreateCounter<int>(
            "create_tasklet_count",
            description: "The number of Tasklets created."
        );

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
            TaskletTelemetry.AddEvent([], "tasklet.create.unauthorized");

            return TypedResults.Unauthorized();
        }

        var validationError = TaskletRequestValidation.Validate(request);

        if (validationError is not null)
        {
            TaskletTelemetry.AddEvent([], "tasklet.create.validation_failed");

            return TypedResults.BadRequest(validationError);
        }

        var created = await storage.CreateTaskletAsync(request.ToTasklet(userId));

        CreateCounter.Add(1);
        TaskletTelemetry.AddEvent([("tasklet.id", created.Id)], "tasklet.create.succeeded");

        return TypedResults.Created($"/v1/tasklets/{created.Id}", created.ToResponse());
    }
}
