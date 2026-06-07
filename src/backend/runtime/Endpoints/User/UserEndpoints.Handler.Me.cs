using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Runtime.Endpoints;

/// <summary>
/// Gets the current user's information based on the authentication claims.
/// </summary>
public class GetMeHandler : IEndpointHandler
{
    /// <summary>
    /// Handles the request to get the current user's information.
    /// </summary>
    public Results<Ok<UserInfoResponse>, UnauthorizedHttpResult> Handle(ClaimsPrincipal user)
    {
        var userId = user?.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
        var email = user?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        if (userId == null || email == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new UserInfoResponse(userId, email));
    }
}
