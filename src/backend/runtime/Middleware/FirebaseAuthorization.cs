using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Tasklet.Runtime.Middleware;

/// <summary>
/// Authentication options for Firebase bearer token validation.
/// </summary>
public class FirebaseAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// ASP.NET Core authentication handler that validates Firebase ID tokens.
/// </summary>
public class FirebaseAuthenticationHandler(
    IOptionsMonitor<FirebaseAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder
) : AuthenticationHandler<FirebaseAuthenticationOptions>(options, loggerFactory, encoder)
{
    /// <summary>
    /// The authentication scheme used for Firebase bearer tokens.
    /// </summary>
    public const string SchemeName = "Firebase";

    /// <summary>
    /// Authenticates the request by validating the Firebase bearer token.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var authorization = authorizationHeader.FirstOrDefault();

        if (
            authorization == null
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            return AuthenticateResult.Fail("Authorization header must use the Bearer scheme.");
        }

        var token = authorization["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Bearer token is empty.");
        }

        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

            var claims = decodedToken.Claims.Select(claim => new Claim(
                claim.Key,
                Convert.ToString(claim.Value) ?? ""
            ));

            var identity = new ClaimsIdentity(claims, SchemeName);

            if (!identity.HasClaim(claim => claim.Type == "user_id"))
            {
                identity.AddClaim(new Claim("user_id", decodedToken.Uid));
            }

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex);
        }
    }
}
