using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Runtime.Endpoints;

namespace Tasklet.Tests.EndpointTests;

/// <summary>
/// Test cases for the user endpoints
///
/// dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/UserEndpointTests/*"
/// </summary>
public class UserEndpointTests
{
    [Test]
    public async Task GetMe_ReturnsUnauthorized_WhenUnauthenticated()
    {
        // Guards that when a user with no claims is passed, returns an
        // unauthorized result
        var handler = new GetMeHandler();

        // User has no claims.
        var result = handler.Handle(new ClaimsPrincipal());

        await Assert.That(result.Result).IsOfType(typeof(UnauthorizedHttpResult));
    }

    [Test]
    public async Task GetMe_ReturnsUserInfo_WhenAuthenticated()
    {
        // Guards that when a user with valid claims is passed, returns an
        // Ok result with the expected user info.
        var handler = new GetMeHandler();

        var claims = new List<Claim> { new("user_id", "123"), new("email", "example@example.org") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var result = handler.Handle(user);

        await Assert.That(result.Result).IsOfType(typeof(Ok<UserInfoResponse>));

        var okResult = result.Result as Ok<UserInfoResponse>;

        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult!.Value!.UserId).IsEqualTo("123");
        await Assert.That(okResult!.Value!.Email).IsEqualTo("example@example.org");
    }
}
