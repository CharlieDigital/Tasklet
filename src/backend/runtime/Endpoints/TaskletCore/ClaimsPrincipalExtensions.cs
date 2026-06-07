using System.Security.Claims;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Claim helpers for Tasklet endpoint handlers.
/// </summary>
/// <remarks>
/// Tasklet routes use the Firebase `user_id` claim as the owner key. Keeping the
/// lookup here makes each handler follow the same rule and avoids small copy and
/// paste differences between list, create, update, and delete flows.
/// </remarks>
internal static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal? user)
    {
        /// <summary>
        /// Gets the authenticated Tasklet user ID claim.
        /// </summary>
        public string? GetTaskletUserId()
        {
            var userId = user?.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value;

            return string.IsNullOrWhiteSpace(userId) ? null : userId;
        }
    }
}
