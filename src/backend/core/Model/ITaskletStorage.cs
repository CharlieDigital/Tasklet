using System.Linq.Expressions;

namespace Tasklet.Core.Model;

/// <summary>
/// Storage boundary for Tasklet persistence.
/// </summary>
/// <remarks>
/// Core depends on this abstraction so runtime code can use Tasklets without
/// taking a direct dependency on a specific database provider.
/// </remarks>
public interface ITaskletStorage
{
    /// <summary>
    /// Gets a user's Tasklets with optional paging and sort selection.
    /// </summary>
    /// <param name="userId">Owner identifier used to scope the query.</param>
    /// <param name="skip">Number of matching Tasklets to skip for paging.</param>
    /// <param name="take">Maximum number of matching Tasklets to return.</param>
    /// <param name="orderBy">
    /// Expression over <see cref="ISortableTasklet"/>; use direct property
    /// access only so storage providers can translate it.
    /// </param>
    /// <param name="filter">
    /// Optional provider-translated filter text for searching Tasklets.
    /// </param>
    /// <returns>The requested page of Tasklets owned by the user.</returns>
    Task<List<Tasklet>> GetTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = 25,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        string? filter = null
    );

    /// <summary>
    /// Gets a Tasklet by its unique identifier.
    /// </summary>
    /// <param name="id">Tasklet identifier.</param>
    /// <returns>
    /// The matching Tasklet, or `null` when it does not exist.
    /// </returns>
    Task<Tasklet?> GetTaskletByIdAsync(Guid id);

    /// <summary>
    /// Persists a new Tasklet.
    /// </summary>
    /// <param name="tasklet">Tasklet to create.</param>
    /// <returns>
    /// The created Tasklet after provider-side defaults or mappings are applied.
    /// </returns>
    Task<Tasklet> CreateTaskletAsync(Tasklet tasklet);

    /// <summary>
    /// Persists changes to an existing Tasklet.
    /// </summary>
    /// <param name="tasklet">Tasklet containing the updated values.</param>
    Task UpdateTaskletAsync(Tasklet tasklet);

    /// <summary>
    /// Deletes a Tasklet by its unique identifier.
    /// </summary>
    /// <param name="id">Tasklet identifier.</param>
    /// <returns>The number of deleted Tasklets.</returns>
    Task<int> DeleteTaskletAsync(Guid id);

    /// <summary>
    /// Performs storage provider initialization such as running migrations on startup.
    /// </summary>
    Task InitializeAsync();

    // TODO: More advanced queries; basic CRUD first
}
