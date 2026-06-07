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
    /// <param name="sortDirection">Direction used for the requested sort member.</param>
    /// <param name="filter">
    /// Optional provider-translated filter text for searching Tasklets.
    /// </param>
    /// <returns>The requested page of Tasklets owned by the user.</returns>
    Task<List<Tasklet>> GetTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = 25,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending,
        string? filter = null
    );

    /// <summary>
    /// Gets a user's pinned Tasklets with optional paging and sort selection.
    /// </summary>
    /// <param name="userId">Owner identifier used to scope the query.</param>
    /// <param name="skip">Number of matching pinned Tasklets to skip for paging.</param>
    /// <param name="take">Maximum number of matching pinned Tasklets to return.</param>
    /// <param name="orderBy">
    /// Expression over <see cref="ISortableTasklet"/>; use direct property
    /// access only so storage providers can translate it.
    /// </param>
    /// <param name="sortDirection">Direction used for the requested sort member.</param>
    /// <returns>The requested page of pinned Tasklets owned by the user.</returns>
    Task<List<Tasklet>> GetPinnedTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = 25,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending
    );

    /// <summary>
    /// Gets a user's completed Tasklets with optional paging and sort selection.
    /// </summary>
    /// <param name="userId">Owner identifier used to scope the query.</param>
    /// <param name="skip">Number of matching completed Tasklets to skip for paging.</param>
    /// <param name="take">Maximum number of matching completed Tasklets to return.</param>
    /// <param name="orderBy">
    /// Expression over <see cref="ISortableTasklet"/>; use direct property
    /// access only so storage providers can translate it.
    /// </param>
    /// <param name="sortDirection">Direction used for the requested sort member.</param>
    /// <returns>The requested page of completed Tasklets owned by the user.</returns>
    Task<List<Tasklet>> GetDoneTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = 25,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending
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
    /// Sets the pinned state of a Tasklet owned by a user.
    /// </summary>
    /// <param name="id">Tasklet identifier.</param>
    /// <param name="userId">Owner identifier used to scope the update.</param>
    /// <param name="pinned">Pinned value to persist.</param>
    /// <returns>The updated Tasklet, or `null` when no owned Tasklet matches.</returns>
    Task<Tasklet?> SetTaskletPinnedAsync(Guid id, string userId, bool pinned);

    /// <summary>
    /// Marks a Tasklet owned by a user as completed.
    /// </summary>
    /// <param name="id">Tasklet identifier.</param>
    /// <param name="userId">Owner identifier used to scope the update.</param>
    /// <param name="completedAtUtc">Completion timestamp to persist.</param>
    /// <returns>The updated Tasklet, or `null` when no owned Tasklet matches.</returns>
    Task<Tasklet?> CompleteTaskletAsync(Guid id, string userId, DateTime completedAtUtc);

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
