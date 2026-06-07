using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tasklet.Core;
using Tasklet.Core.Model;

namespace Tasklet.Sqlite;

/// <summary>
/// Service registration for SQLite-backed Tasklet storage.
/// </summary>
/// <remarks>
/// Runtime owns the choice of storage provider, but the SQLite project owns how
/// SQLite is wired. This keeps EF Core setup close to the provider and keeps the
/// API handlers behind <see cref="ITaskletStorage"/>. When startup calls this
/// method, requests will get one scoped context and provider for each request.
/// </remarks>
public static class SqliteServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the SQLite Tasklet storage provider to dependency injection.
        /// </summary>
        /// <param name="storage">Storage settings from runtime configuration.</param>
        /// <returns>The service collection for continued setup.</returns>
        public IServiceCollection AddTaskletSqliteStorage(StorageSettings storage)
        {
            if (string.IsNullOrWhiteSpace(storage.ConnectionString))
            {
                throw new InvalidOperationException("SQLite storage requires a connection string.");
            }

            services.AddDbContext<SqliteContext>(options =>
                options.UseSqlite(storage.ConnectionString)
            );
            services.AddScoped<ITaskletStorage, SqliteStorageProvider>();

            return services;
        }
    }
}
