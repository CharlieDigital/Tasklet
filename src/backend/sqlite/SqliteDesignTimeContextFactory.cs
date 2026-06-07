using Microsoft.EntityFrameworkCore.Design;

namespace Tasklet.Sqlite;

/// <summary>
/// Creates a Sqlite context for EF Core migration tooling.
/// </summary>
/// <remarks>
/// EF migration commands need a context before the runtime app is running. This
/// factory gives the tools a safe design-time database path so they can read the
/// model and write migration files. Runtime still uses the connection string
/// from app settings, so this class is only part of the build-time flow.
/// </remarks>
public sealed class SqliteDesignTimeContextFactory : IDesignTimeDbContextFactory<SqliteContext>
{
    /// <inheritdoc/>
    public SqliteContext CreateDbContext(string[] args) =>
        new(SqliteContext.MakeOptions("Data Source=../../../.data/tasklet-design-time.db"));
}
