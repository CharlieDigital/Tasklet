using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace Tasklet.Runtime.Middleware;

/// <summary>
/// Attribute that applies the Firebase authorization check.
/// </summary>
public class FirebaseAuthorizationAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FirebaseAuthorizationAttribute"/> class.
    /// </summary>
    public FirebaseAuthorizationAttribute()
        : base(typeof(FirebaseAuthorizationFilter)) { }
}

/// <summary>
/// Authorization filter which will ensure that the call includes a valid Firebase
/// auth token.
///
/// See: https://firebase.google.com/docs/auth/admin/verify-id-tokens
/// See: https://firebase.google.com/docs/emulator-suite/connect_auth#environment-variable
/// </summary>
public class FirebaseAuthorizationFilter(ILogger<FirebaseAuthorizationFilter> logger)
    : IAuthorizationFilter
{
    /// <summary>
    /// Verify that the user has a valid authentication token on the call.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var headers = context.HttpContext.Request.Headers;

        if (!headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader))
        {
            context.Result = new UnauthorizedObjectResult("Missing authorization header");
            return;
        }

        try
        {
            var token = (authorizationHeader.First() ?? "")[7..];

            var decodedTokenClaims = FirebaseAuth
                .DefaultInstance.VerifyIdTokenAsync(token)
                .Result.Claims;

            var claims = new List<Claim>();

            claims.AddRange(
                decodedTokenClaims.Select(c => new Claim(c.Key, Convert.ToString(c.Value) ?? ""))
            );

            var localIdentity = new ClaimsIdentity(claims);

            context.HttpContext.User.AddIdentity(localIdentity);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to decode the token: {Token}",
                headers.Authorization.First() ?? ""
            );

            context.Result = new UnauthorizedObjectResult("Token validation failed with an error.");
        }
    }
}
