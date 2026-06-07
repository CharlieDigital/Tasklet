using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tasklet.Core;
using Tasklet.Core.Model;

namespace Tasklet.Sqlite;

// TODO: Implement, test, and document this class.

public class SqliteStorageProvider(
    ILogger<SqliteStorageProvider> logger,
    IOptions<AppSettings> options
) : ITaskletStorage
{
    public Task<Core.Model.Tasklet> CreateTaskletAsync(Core.Model.Tasklet tasklet)
    {
        throw new NotImplementedException();
    }

    public Task<int> DeleteTaskletAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Core.Model.Tasklet?> GetTaskletByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<Core.Model.Tasklet>> GetTaskletsForUserAsync(
        string userId,
        int skip = 0,
        int take = 25,
        Expression<Func<ISortableTasklet, object?>>? orderBy = null,
        string? filter = null
    )
    {
        throw new NotImplementedException();
    }

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateTaskletAsync(Core.Model.Tasklet tasklet)
    {
        throw new NotImplementedException();
    }
}
