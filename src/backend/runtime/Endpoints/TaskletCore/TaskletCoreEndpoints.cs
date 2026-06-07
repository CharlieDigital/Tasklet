using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tasklet.Core.Endpoints;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Core CRUD endpoints for Tasklet.
/// </summary>
public class TaskletCoreEndpoints : IEndpoint
{
    /// <summary>
    /// Maps the endpoints for Tasklet core functionality.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasklets").WithTags("Tasklet");

        group
            .MapGet(
                "/",
                (
                    ClaimsPrincipal user,
                    [FromServices] ListTaskletsHandler handler,
                    int skip = 0,
                    int take = 25,
                    TaskletSortField? sort = null,
                    SortDirection direction = SortDirection.Descending,
                    string? filter = null
                ) => handler.Handle(user, skip, take, sort, direction, filter)
            )
            .WithName("ListTasklets")
            .WithDescription("Gets the current user's Tasklets.");

        group
            .MapGet(
                "/pinned",
                (
                    ClaimsPrincipal user,
                    [FromServices] PinnedTaskletsHandler handler,
                    int skip = 0,
                    int take = 25,
                    TaskletSortField? sort = null,
                    SortDirection direction = SortDirection.Descending
                ) => handler.Handle(user, skip, take, sort, direction)
            )
            .WithName("ListPinnedTasklets")
            .WithDescription("Gets the current user's pinned Tasklets.");

        group
            .MapGet(
                "/done",
                (
                    ClaimsPrincipal user,
                    [FromServices] DoneTaskletsHandler handler,
                    int skip = 0,
                    int take = 25,
                    TaskletSortField? sort = null,
                    SortDirection direction = SortDirection.Descending
                ) => handler.Handle(user, skip, take, sort, direction)
            )
            .WithName("ListDoneTasklets")
            .WithDescription("Gets the current user's completed Tasklets.");

        group
            .MapGet(
                "/{id:guid}",
                (ClaimsPrincipal user, Guid id, [FromServices] GetTaskletHandler handler) =>
                    handler.Handle(user, id)
            )
            .WithName("GetTasklet")
            .WithDescription("Gets one Tasklet owned by the current user.");

        group
            .MapPost(
                "/",
                (
                    ClaimsPrincipal user,
                    [FromBody] CreateTaskletRequest request,
                    [FromServices] CreateTaskletHandler handler
                ) => handler.Handle(user, request)
            )
            .WithName("CreateTasklet")
            .WithDescription("Creates a Tasklet for the current user.");

        group
            .MapPut(
                "/{id:guid}",
                (
                    ClaimsPrincipal user,
                    Guid id,
                    [FromBody] UpdateTaskletRequest request,
                    [FromServices] UpdateTaskletHandler handler
                ) => handler.Handle(user, id, request)
            )
            .WithName("UpdateTasklet")
            .WithDescription("Updates a Tasklet owned by the current user.");

        group
            .MapPut(
                "/{id:guid}/pin",
                (ClaimsPrincipal user, Guid id, [FromServices] PinTaskletHandler handler) =>
                    handler.Pin(user, id)
            )
            .WithName("PinTasklet")
            .WithDescription("Pins a Tasklet owned by the current user.");

        group
            .MapPut(
                "/{id:guid}/unpin",
                (ClaimsPrincipal user, Guid id, [FromServices] PinTaskletHandler handler) =>
                    handler.Unpin(user, id)
            )
            .WithName("UnpinTasklet")
            .WithDescription("Unpins a Tasklet owned by the current user.");

        group
            .MapPut(
                "/{id:guid}/complete",
                (ClaimsPrincipal user, Guid id, [FromServices] CompleteTaskletHandler handler) =>
                    handler.Handle(user, id)
            )
            .WithName("CompleteTasklet")
            .WithDescription("Marks a Tasklet owned by the current user as completed.");

        group
            .MapDelete(
                "/{id:guid}",
                (ClaimsPrincipal user, Guid id, [FromServices] DeleteTaskletHandler handler) =>
                    handler.Handle(user, id)
            )
            .WithName("DeleteTasklet")
            .WithDescription("Deletes a Tasklet owned by the current user.");
    }
}
