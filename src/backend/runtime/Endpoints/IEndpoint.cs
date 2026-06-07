namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Marker interface for working with minimal API endpoints and simplifying registration.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the endpoints to the given app builder.  We'll use this to simplify
    /// global registration of endpoints.
    /// </summary>
    /// <param name="app">The application builder used for registration.</param>
    void MapEndpoints(IEndpointRouteBuilder app);
}

/// <summary>
/// Marker interface for endpoint handler classes.
/// </summary>
public interface IEndpointHandler { }
