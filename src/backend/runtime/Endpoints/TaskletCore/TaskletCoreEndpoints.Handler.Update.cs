using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;
using Tasklet.Core.Telemetry;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Updates a Tasklet owned by the authenticated user.
/// </summary>
public class UpdateTaskletHandler(ITaskletStorage storage) : IEndpointHandler
{
    private static readonly Counter<int> UpdateCounter =
        TaskletTelemetry.Metrics.CreateCounter<int>(
            "update_tasklet_count",
            description: "The number of Tasklets updated."
        );

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
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.update.unauthorized");

            return TypedResults.Unauthorized();
        }

        var validationError = TaskletRequestValidation.Validate(request);

        if (validationError is not null)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.update.validation_failed");

            return TypedResults.BadRequest(validationError);
        }

        var existing = await storage.GetTaskletByIdAsync(id);

        if (existing is null || existing.UserId != userId)
        {
            TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.update.not_found");

            return TypedResults.NotFound();
        }

        request.ApplyTo(existing);
        await storage.UpdateTaskletAsync(existing);

        UpdateCounter.Add(1);
        TaskletTelemetry.AddEvent([("tasklet.id", id)], "tasklet.update.succeeded");

        return TypedResults.Ok(existing.ToResponse());
    }
}
