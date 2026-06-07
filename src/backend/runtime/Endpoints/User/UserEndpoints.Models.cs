namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// A record representing user information returned by the /me endpoint.
/// </summary>
/// <param name="UserId">The ID of the user.</param>
/// <param name="Email">The email of the user.</param>
public record UserInfoResponse(string UserId, string Email);
