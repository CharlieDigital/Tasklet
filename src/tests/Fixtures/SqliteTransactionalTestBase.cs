using Microsoft.EntityFrameworkCore.Storage;
using Tasklet.Sqlite;

namespace Tasklet.Tests.Fixtures;

/// <summary>
/// Base class for SQLite tests that need rollback after each test.
/// </summary>
/// <remarks>
/// Each test gets its own context and transaction, but all tests share the same
/// migrated database file from <see cref="SqliteDatabaseFixture"/>. The provider
/// uses the same context as the test, so every insert, update, and delete is
/// inside the transaction. Cleanup rolls the transaction back so tests do not
/// depend on data left behind by earlier tests.
/// </remarks>
public abstract class SqliteTransactionalTestBase(SqliteDatabaseFixture fixture)
{
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Context enlisted in the current test transaction.
    /// </summary>
    protected SqliteContext Context { get; private set; } = null!;

    /// <summary>
    /// Storage provider under test, sharing the current transaction context.
    /// </summary>
    protected SqliteStorageProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Opens a context and transaction before each test.
    /// </summary>
    [Before(Test)]
    public async Task Before()
    {
        Context = fixture.CreateContext();
        Provider = fixture.CreateProvider(Context);
        _transaction = await Context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// After test hook that rolls back the transaction and disposes the context.
    /// </summary>
    [After(Test)]
    public async Task Cleanup()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        await Context.DisposeAsync();
    }
}
