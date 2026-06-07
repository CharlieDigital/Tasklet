using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tasklet.Core;
using Tasklet.Sqlite;
using TUnit.Core.Interfaces;

namespace Tasklet.Tests.Fixtures;

/// <summary>
/// Database fixture that initializes a Sqlite test database for integration testing.
/// </summary>
/// <remarks>
/// Creates a temporary file-based Sqlite database and initializes the
/// <see cref="SqliteStorageProvider"/> against it so tests can exercise the full
/// provider interface without any mocking.  The database file is deleted when the
/// fixture is disposed.
/// </remarks>
public class SqliteDatabaseFixture : IAsyncInitializer, IAsyncDisposable
{
    private string _dbPath = string.Empty;
    private SqliteStorageProvider? _provider;

    /// <summary>
    /// Connection string for the temporary Sqlite database.
    /// </summary>
    public string ConnectionString => $"Data Source={_dbPath}";

    /// <summary>
    /// The initialized storage provider under test.
    /// </summary>
    public SqliteStorageProvider Provider => _provider!;

    /// <summary>
    /// Creates a new context against the temporary Sqlite database.
    /// </summary>
    /// <returns>A Sqlite context for direct assertions.</returns>
    public SqliteContext CreateContext() => new(SqliteContext.MakeOptions(ConnectionString));

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"tasklet-test-{Guid.NewGuid():N}.db");

        var options = Options.Create(
            new AppSettings(
                Firebase: null,
                Auth: null,
                Storage: new StorageSettings(StorageProvider.Sqlite, ConnectionString)
            )
        );

        _provider = new SqliteStorageProvider(NullLogger<SqliteStorageProvider>.Instance, options);

        await _provider.InitializeAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        GC.SuppressFinalize(this);

        await Task.CompletedTask;
    }
}
