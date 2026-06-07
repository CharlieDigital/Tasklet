using Microsoft.EntityFrameworkCore;
using Tasklet.Core.Model;

namespace Tasklet.Sqlite;

/// <summary>
/// EF Core context for the Sqlite storage provider.
/// </summary>
/// <remarks>
/// This class is the only place where the core Tasklet model is shaped into a
/// SQLite table. That keeps the model simple and lets other storage providers
/// use the same model later without taking EF Core details with them. The
/// provider calls this context for reads and writes, and migrations use this
/// mapping to create the real database schema.
/// </remarks>
public class SqliteContext(DbContextOptions<SqliteContext> options) : DbContext(options)
{
    /// <summary>
    /// Tasklet rows persisted by the Sqlite provider.
    /// </summary>
    public DbSet<Core.Model.Tasklet> Tasklets => Set<Core.Model.Tasklet>();

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
        var tasklet = modelBuilder.Entity<Core.Model.Tasklet>();

        tasklet.ToTable("Tasklets");
        tasklet.HasKey(x => x.Id);
        tasklet.Property(x => x.Id).ValueGeneratedNever();

        tasklet.Property(x => x.UserId).IsRequired().HasMaxLength(100);
        tasklet.Property(x => x.Title).IsRequired().HasMaxLength(200);
        tasklet.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        tasklet.Property(x => x.Status).HasConversion<int>();
        tasklet.Property(x => x.Priority).HasConversion<int>();
        tasklet.Property(x => x.ExplicitOrder).HasMaxLength(64);
        tasklet.Property(x => x.Color).HasConversion<int>();
        tasklet.Property(x => x.CreatedAtUtc).IsRequired();

        tasklet.HasIndex(x => x.UserId);
        tasklet.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        tasklet.HasIndex(x => new { x.UserId, x.Status });
        tasklet.HasIndex(x => new { x.UserId, x.Priority });
        tasklet.HasIndex(x => new { x.UserId, x.DueAtUtc });
        tasklet.HasIndex(x => new
        {
            x.UserId,
            x.Pinned,
            x.ExplicitOrder,
        });
    }
}
