namespace Tasklet.Core.Model;

/// <summary>
/// A user profile in the Tasklet system. This is not the same as
/// the user identity which is already provided by the authentication
/// (we have that in the claim).  This is used to store additional
/// information about the user.
/// </summary>
public class UserProfile
{
    /// <summary>
    /// The unique identifier for the user profile.  We use the
    /// subject claim when this is provisioned.  In this app,
    /// we're just going to create this inline on the /me route
    /// but ideally, we just do this on first account creation.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The users's preferred timezone.  We internally store
    /// all time in UTC so we need to know how to convert it
    /// back to the user's timezone.
    /// </summary>
    public string Timezone { get; set; } = "UTC";
    // TODO: Implement UI for this later 👆
}
