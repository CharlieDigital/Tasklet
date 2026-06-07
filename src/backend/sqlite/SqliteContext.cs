using Microsoft.EntityFrameworkCore;

namespace Tasklet.Sqlite;

public class SqliteContext(DbContextOptions<SqliteContext> options) : DbContext(options)
{
    /// <summary>
    /// Makes the default options for the Sqlite database context.
    /// </summary>
    /// <param name="connectionString">The optional connection string to use.</param>
    /// <returns>The default options</returns>
    public static DbContextOptions<SqliteContext> MakeOptions(
        string? connectionString = "Data Source=../../../.data/tasklet-local-sqlite.db"
    ) => new DbContextOptionsBuilder<SqliteContext>().UseSqlite(connectionString).Options;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Model mapping, add indexes
        base.OnModelCreating(modelBuilder);
    }
}
