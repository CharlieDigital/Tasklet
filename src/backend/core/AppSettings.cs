namespace Tasklet.Core;

/// <summary>
/// Record class for application settings deserialization from configuration.
/// </summary>
public record AppSettings(FirebaseSettings? Firebase, AuthSettings? Auth, StorageSettings? Storage);

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

/// <summary>
/// The storage settings for the MCP server.
/// </summary>
public record StorageSettings(StorageProvider Provider, string ConnectionString);

/// <summary>
/// The storage provider type to use.
/// </summary>
public enum StorageProvider
{
    // TODO(production): Add other providers here like Postgres, Firestore, etc.

    /// <summary>
    /// Sqlite storage provider (default)
    /// </summary>
    Sqlite = 0,
}
