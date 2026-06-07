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
            (HttpRequest httpRequest, CancellationToken cancellationToken) =>
            {
                var user = httpRequest.HttpContext?.User;
                var userId = user?.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
                var email = user?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                if (userId == null || email == null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new { UserId = userId, Email = email });
            }
        );
    }
}
