using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// User endpoints.
/// </summary>
public class UserEndpoints : IEndpoint
{
    /// <summary>
    /// Maps the endpoints for accessing user info.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/me",
                (ClaimsPrincipal user, [FromServices] GetMeHandler handler) => handler.Handle(user)
            )
            .WithName("Me")
            .WithTags("User")
            .WithDescription("Gets the current user's information.");
    }
}
