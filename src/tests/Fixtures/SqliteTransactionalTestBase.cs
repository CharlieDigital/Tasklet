using Microsoft.EntityFrameworkCore.Storage;
using Tasklet.Sqlite;

namespace Tasklet.Tests.Fixtures;

public abstract class SqliteTransactionalTestBase(SqliteDatabaseFixture fixture)
{
    private IDbContextTransaction? _transaction;

    protected SqliteContext _context = fixture.CreateContext();

    [Before(Test)]
    public async Task Before()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
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

        await _context.DisposeAsync();
    }
}
