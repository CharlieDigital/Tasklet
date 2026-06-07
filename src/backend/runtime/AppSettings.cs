namespace Tasklet.Runtime;

/// <summary>
/// Record class for application settings deserialization from configuration.
/// </summary>
public record AppSettings(FirebaseSettings Firebase, AuthSettings Auth);

/// <summary>
/// Record class for Firebase settings deserialization from configuration.
/// </summary>
/// <param name="ProjectId">The Firebase project ID.</param>
public record FirebaseSettings(string ProjectId);

/// <summary>
/// The allowed CORS origins for the app, deserialized from configuration.
/// </summary>
/// <param name="AllowedCorsOrigins">
/// An array of allowed CORS origins for the app; can be overridden by environment vars.
/// </param>
public record AuthSettings(string[] AllowedCorsOrigins);
