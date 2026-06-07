using Microsoft.Extensions.Logging.Abstractions;
using Tasklet.Sqlite;
using TUnit.Core.Interfaces;

namespace Tasklet.Tests.Fixtures;

/// <summary>
/// Database fixture that initializes a Sqlite test database for integration testing.
/// </summary>
/// <remarks>
/// The storage tests need a real SQLite file because migrations, indexes, LIKE
/// filters, and deletes should run the same way they do in the app. This fixture
/// creates one temporary database for the test class, runs provider
/// initialization once, and gives each test a context pointed at that database.
/// It deletes the database and SQLite sidecar files when the test run is done.
/// </remarks>
public class SqliteDatabaseFixture : IAsyncInitializer, IAsyncDisposable
{
    private string _dbPath = string.Empty;

    /// <summary>
    /// Connection string for the temporary Sqlite database.
    /// </summary>
    public string ConnectionString => $"Data Source={_dbPath}";

    /// <summary>
    /// Creates a provider that uses the supplied context.
    /// </summary>
    /// <param name="context">Context owned by the current test transaction.</param>
    /// <returns>A provider wired to the supplied context.</returns>
    public SqliteStorageProvider CreateProvider(SqliteContext context)
        => new(context, NullLogger<SqliteStorageProvider>.Instance);

    /// <summary>
    /// Creates a new context against the temporary Sqlite database.
    /// </summary>
    /// <returns>A Sqlite context for direct assertions.</returns>
    public SqliteContext CreateContext() => new(SqliteContext.MakeOptions(ConnectionString));

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tasklet-test-{Guid.NewGuid():N}.db");

        await using var context = CreateContext();
        var provider = CreateProvider(context);

        await provider.InitializeAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        foreach (var path in new[] { _dbPath, $"{_dbPath}-shm", $"{_dbPath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        GC.SuppressFinalize(this);

        await Task.CompletedTask;
    }
}
