using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tasklet.Core.Model;

namespace Tasklet.Sqlite;

/// <summary>
/// Sqlite implementation of the Tasklet storage boundary.
/// </summary>
/// <remarks>
/// Runtime code talks to <see cref="ITaskletStorage"/>, and this class is the
/// SQLite version behind that interface. It owns database work like migrations,
/// paging, filtering, sorting, and saving changes. The handlers should not know
/// about EF Core or SQLite, so this class is the translation layer between the
/// app's Tasklet model and the database.
/// </remarks>
public partial class SqliteStorageProvider(
    SqliteContext context,
    ILogger<SqliteStorageProvider> logger
) : ITaskletStorage
{
    private const int DefaultTake = 25;
    private const int MaxTake = 100;
    private const string LikeEscapeCharacter = "\\";

    /// <inheritdoc/>
    public async Task<Core.Model.Tasklet> CreateTaskletAsync(Core.Model.Tasklet tasklet)
    {
        if (tasklet.Id == Guid.Empty)
        {
            tasklet.Id = Guid.CreateVersion7();
        }

        if (tasklet.CreatedAtUtc == default)
        {
            tasklet.CreatedAtUtc = DateTime.UtcNow;
        }

        tasklet.CreatedAtUtc = NormalizeUtc(tasklet.CreatedAtUtc);
        tasklet.CompletedAtUtc = NormalizeUtc(tasklet.CompletedAtUtc);
        tasklet.DueAtUtc = NormalizeUtc(tasklet.DueAtUtc);

        context.Tasklets.Add(tasklet);
        await context.SaveChangesAsync();

        // Return a detached domain object so later caller mutations do not
        // accidentally change EF's tracked original values in this context.
        context.Entry(tasklet).State = EntityState.Detached;

        return tasklet;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteTaskletAsync(Guid id) =>
        await context.Tasklets.Where(tasklet => tasklet.Id == id).ExecuteDeleteAsync();

    /// <inheritdoc/>
    public async Task<Core.Model.Tasklet?> GetTaskletByIdAsync(Guid id) =>
        await context.Tasklets.AsNoTracking().SingleOrDefaultAsync(tasklet => tasklet.Id == id);

    /// <inheritdoc/>
    public async Task<List<Core.Model.Tasklet>> GetPinnedTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = DefaultTake,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending
    )
    {
        var (normalizedSkip, normalizedTake) = NormalizePaging(skip, take);

        var query = context
            .Tasklets.AsNoTracking()
            .Where(tasklet => tasklet.UserId == userId && tasklet.Pinned);

        return await ApplyTaskletOrdering(query, orderBy, sortDirection)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Core.Model.Tasklet>> GetDoneTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = DefaultTake,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending
    )
    {
        var (normalizedSkip, normalizedTake) = NormalizePaging(skip, take);

        var query = context
            .Tasklets.AsNoTracking()
            .Where(tasklet => tasklet.UserId == userId && tasklet.Status == Status.Completed);

        return await ApplyTaskletOrdering(query, orderBy, sortDirection)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<Core.Model.Tasklet>> GetTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = DefaultTake,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        SortDirection sortDirection = SortDirection.Descending,
        string? filter = null
    )
    {
        var (normalizedSkip, normalizedTake) = NormalizePaging(skip, take);

        var query = context.Tasklets.AsNoTracking().Where(tasklet => tasklet.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var escapedFilter = EscapeLikePattern(filter.Trim());
            var pattern = $"%{escapedFilter}%";

            query = query.Where(tasklet =>
                EF.Functions.Like(tasklet.Title, pattern, LikeEscapeCharacter)
                || EF.Functions.Like(tasklet.Body, pattern, LikeEscapeCharacter)
            );
        }

        return await ApplyTaskletOrdering(query, orderBy, sortDirection)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var connectionString = context.Database.GetConnectionString();
        EnsureDatabaseDirectory(connectionString);

        LogMigratingDatabase(connectionString ?? "<unknown>");
        await context.Database.MigrateAsync();
        LogDatabaseReady(connectionString ?? "<unknown>");
    }

    /// <inheritdoc/>
    public async Task<Core.Model.Tasklet?> SetTaskletPinnedAsync(
        Guid id,
        string userId,
        bool pinned
    )
    {
        var existing = await context.Tasklets.SingleOrDefaultAsync(row =>
            row.Id == id && row.UserId == userId
        );

        if (existing is null)
        {
            return null;
        }

        existing.Pinned = pinned;
        await context.SaveChangesAsync();
        context.Entry(existing).State = EntityState.Detached;

        return existing;
    }

    /// <inheritdoc/>
    public async Task<Core.Model.Tasklet?> CompleteTaskletAsync(
        Guid id,
        string userId,
        DateTime completedAtUtc
    )
    {
        var existing = await context.Tasklets.SingleOrDefaultAsync(row =>
            row.Id == id && row.UserId == userId
        );

        if (existing is null)
        {
            return null;
        }

        existing.Status = Status.Completed;
        existing.CompletedAtUtc = NormalizeUtc(completedAtUtc);
        await context.SaveChangesAsync();
        context.Entry(existing).State = EntityState.Detached;

        return existing;
    }

    /// <inheritdoc/>
    public async Task UpdateTaskletAsync(Core.Model.Tasklet tasklet)
    {
        var existing = await context.Tasklets.SingleOrDefaultAsync(row => row.Id == tasklet.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException($"Tasklet '{tasklet.Id}' was not found.");
        }

        existing.Title = tasklet.Title;
        existing.Body = tasklet.Body;
        existing.Status = tasklet.Status;
        existing.Priority = tasklet.Priority;
        existing.ExplicitOrder = tasklet.ExplicitOrder;
        existing.Pinned = tasklet.Pinned;
        existing.Color = tasklet.Color;
        existing.CompletedAtUtc = NormalizeUtc(tasklet.CompletedAtUtc);
        existing.DueAtUtc = NormalizeUtc(tasklet.DueAtUtc);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates the database folder for file-backed connection strings.
    /// </summary>
    private static void EnsureDatabaseDirectory(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (
            string.IsNullOrWhiteSpace(dataSource)
            || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Normalizes paging at the storage boundary so every caller gets bounded queries.
    /// </summary>
    private static (int Skip, int Take) NormalizePaging(int skip, int take) =>
        (Math.Max(skip, 0), Math.Min(take <= 0 ? DefaultTake : take, MaxTake));

    /// <summary>
    /// Stores all DateTime values as UTC to avoid Sqlite timezone ambiguity.
    /// </summary>
    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    /// <summary>
    /// Stores nullable DateTime values as UTC to avoid Sqlite timezone ambiguity.
    /// </summary>
    private static DateTime? NormalizeUtc(DateTime? value) =>
        value.HasValue ? NormalizeUtc(value.Value) : null;

    /// <summary>
    /// Escapes Sqlite LIKE wildcard characters so free-text filters remain literal.
    /// </summary>
    private static string EscapeLikePattern(string value) =>
        value.Replace(LikeEscapeCharacter, "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// Applies the pinned-first ordering and a stable Id tie-breaker.
    /// </summary>
    private static IOrderedQueryable<Core.Model.Tasklet> ApplyTaskletOrdering(
        IQueryable<Core.Model.Tasklet> query,
        Expression<Func<ISortableTasklet, object?>>? orderBy,
        SortDirection sortDirection
    )
    {
        var ordered = query.OrderByDescending(tasklet => tasklet.Pinned);

        if (orderBy is null)
        {
            return ordered
                .ThenByDescending(tasklet => tasklet.CreatedAtUtc)
                .ThenBy(tasklet => tasklet.Id);
        }

        return GetSortableMemberName(orderBy) switch
        {
            nameof(ISortableTasklet.Title) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.Title
            ),
            nameof(ISortableTasklet.Status) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.Status
            ),
            nameof(ISortableTasklet.Priority) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.Priority
            ),
            nameof(ISortableTasklet.ExplicitOrder) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.ExplicitOrder
            ),
            nameof(ISortableTasklet.CreatedAtUtc) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.CreatedAtUtc
            ),
            nameof(ISortableTasklet.CompletedAtUtc) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.CompletedAtUtc
            ),
            nameof(ISortableTasklet.DueAtUtc) => ApplyDirection(
                ordered,
                sortDirection,
                tasklet => tasklet.DueAtUtc
            ),
            var memberName => throw new NotSupportedException(
                $"Sorting by '{memberName}' is not supported."
            ),
        };
    }

    /// <summary>
    /// Converts the caller's direction enum into an EF-translatable ordering call.
    /// </summary>
    private static IOrderedQueryable<Core.Model.Tasklet> ApplyDirection<TProperty>(
        IOrderedQueryable<Core.Model.Tasklet> ordered,
        SortDirection sortDirection,
        Expression<Func<Core.Model.Tasklet, TProperty>> keySelector
    ) =>
        sortDirection switch
        {
            SortDirection.Ascending => ordered.ThenBy(keySelector).ThenBy(tasklet => tasklet.Id),
            SortDirection.Descending => ordered
                .ThenByDescending(keySelector)
                .ThenBy(tasklet => tasklet.Id),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sortDirection),
                sortDirection,
                "Unsupported sort direction."
            ),
        };

    /// <summary>
    /// Reads a direct sortable member expression from the core abstraction.
    /// </summary>
    private static string GetSortableMemberName(Expression<Func<ISortableTasklet, object?>> orderBy)
    {
        var expression = orderBy.Body is UnaryExpression unary ? unary.Operand : orderBy.Body;

        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new NotSupportedException(
            "Sort expressions must be direct ISortableTasklet property access."
        );
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Migrating Sqlite Tasklet database at {ConnectionString}."
    )]
    private partial void LogMigratingDatabase(string connectionString);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Sqlite Tasklet database is ready at {ConnectionString}."
    )]
    private partial void LogDatabaseReady(string connectionString);
}
