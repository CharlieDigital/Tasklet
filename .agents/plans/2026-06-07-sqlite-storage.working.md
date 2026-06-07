# Sqlite Storage Provider Working Plan

## Source Plan

- Draft: `.agents/plans/2026-06-07-sqlite-storage.draft.md`
- Objective: implement the Sqlite-backed Tasklet storage provider, verify it with TUnit integration tests, expose Tasklet CRUD through runtime Minimal API endpoints, and regenerate frontend API clients.
- Backend API URL for verification: `http://api.localhost:8089`
- Frontend URL for verification: `http://tasklet.localhost:8089`
- Local database target: `.data/tasklet.db`
- Required checkpoint rule from the draft: stop between each phase and ask for a checkpoint before continuing.

## Project Guidance Reviewed

- `README.md`
- `.agents/context/tasklet-architecture.md`
- `.agents/context/tasklet-backend-dotnet.md`
- `.agents/context/tasklet-tunit-testing.md`
- `.agents/context/tasklet-frontend-vue-naiveui.md`
- Draft plan: `.agents/plans/2026-06-07-sqlite-storage.draft.md`

## Current Codebase Findings

- The repo is currently on `main` with existing uncommitted scaffold changes. Implementation must avoid reverting or overwriting unrelated user work.
- The actual frontend path is `src/web`, not `src/frontend`.
- `src/backend/core` owns shared abstractions and models:
  - `Model/ITaskletStorage.cs`
  - `Model/Tasklet.cs`
  - `Model/TaskletEnums.cs`
  - `Model/ISortableTasklet.cs`
  - `AppSettings.cs`
- `TaskletEnums.cs` includes shared enum values, including `SortDirection` for ordered Tasklet queries.
- `ITaskletStorage` already defines the storage contract:
  - `GetTaskletsForUserAsync(...)`
  - `GetTaskletByIdAsync(Guid id)`
  - `CreateTaskletAsync(Tasklet tasklet)`
  - `UpdateTaskletAsync(Tasklet tasklet)`
  - `DeleteTaskletAsync(Guid id)`
  - `InitializeAsync()`
- `Tasklet` is intentionally storage-agnostic. EF mapping belongs in `src/backend/sqlite/SqliteContext.cs`.
- `Tasklet` uses `DateTime` instead of `DateTimeOffset`, matching the backend guidance for SQLite provider limitations.
- `SqliteStorageProvider.cs` and `SqliteContext.cs` are stubs.
- `src/tests/Fixtures/SqliteDatabaseFixture.cs` and `SqliteTransactionalTestBase.cs` exist but should be adjusted so provider tests and transaction rollback use the same `SqliteContext`.
- Runtime endpoint conventions are already established by the User endpoint:
  - `IEndpoint` maps HTTP routes only.
  - `IEndpointHandler` classes encapsulate testable domain/request logic.
  - Tests target handlers directly instead of hosting the full endpoint pipeline.
- `TaskletCoreEndpoints.cs` exists as an empty endpoint stub.
- `SetupServicesExtensions.cs` uses Scrutor to register `IEndpoint` and `IEndpointHandler` implementations from the runtime assembly.
- Runtime currently references only `Tasklet.Core`. It does not reference `Tasklet.Sqlite`.
- `appsettings.json` already has:
  - `AppSettings:Storage:Provider = Sqlite`
  - `AppSettings:Storage:ConnectionString = Data Source=../../../.data/tasklet.db`
- `.gitignore` does not yet ignore `.data/`.
- OpenAPI/Kubb generation is wired through `src/backend/runtime/Tasklet.Runtime.csproj` when `GEN=true` or `GenerateSchema=true`.
- Generated frontend clients live under `src/web/src/api/generated/**` and must not be manually edited.

## Decisions For This Plan

### Runtime Dependency Boundary

Endpoint handlers must depend on `ITaskletStorage`, not `SqliteStorageProvider` or `SqliteContext`.

The runtime composition root may reference `Tasklet.Sqlite` to register the configured provider. This keeps endpoint and domain code behind the core abstraction while still allowing the application to choose the concrete provider from configuration.

Implementation consequence:

- Add a project reference from `src/backend/runtime/Tasklet.Runtime.csproj` to `src/backend/sqlite/Tasklet.Sqlite.csproj`.
- Add a SQLite registration extension in the SQLite project, then call it from `SetupServicesExtensions` or `Program`.
- Keep all SQLite-specific EF Core details out of endpoint handlers.

### Provider Lifetime And DbContext Ownership

Use a scoped EF `SqliteContext` and a scoped `ITaskletStorage` implementation.

Implementation consequence:

- Change `SqliteStorageProvider` to accept `SqliteContext` and `ILogger<SqliteStorageProvider>`.
- Register `SqliteContext` with `AddDbContext<SqliteContext>(...)`.
- Register `ITaskletStorage` as scoped.
- Update tests so each test creates a provider with the same `_context` opened by `SqliteTransactionalTestBase`. This allows transaction rollback to cover provider calls.

Reason:

- The draft already provides a transactional test base. A provider that creates independent contexts per method would not participate in the base transaction and would make tests leak state between cases.

### Schema Initialization

Use EF Core migrations for runtime initialization.

Implementation consequence:

- Create an initial migration in `src/backend/sqlite/Migrations`.
- `SqliteStorageProvider.InitializeAsync()` should ensure the database directory exists and then run `context.Database.MigrateAsync()`.
- Do not use `EnsureCreatedAsync()` in production runtime code because it bypasses migrations.
- Tests may use the same `InitializeAsync()` path to verify the production initialization path against a temp file database.

Fallback rule:

- If migration tooling is unavailable, stop at Phase 1 checkpoint and ask before substituting `EnsureCreatedAsync()`.

### Search Scope

Implement the `filter` parameter as provider-translated SQL using `EF.Functions.Like` over `Title` and `Body` for this plan.

Reason:

- The draft calls for basic CRUD first.
- Full FTS5 support needs virtual table lifecycle, triggers, rebuild behavior, and raw SQL query mapping. That is a meaningful follow-up feature, not a CRUD prerequisite.
- The mapping and indexes should not block a future FTS5 migration.

Out-of-scope for this plan:

- FTS5 virtual tables and triggers.
- Semantic search and embeddings.
- Task heatmap/timeline frontend.
- Drag/drop ordering UI.

### Sorting

Use the core `SortDirection` enum for ordered list queries.

Implementation consequence:

- Update `ITaskletStorage.GetTaskletsForUserAsync(...)` to accept `SortDirection sortDirection = SortDirection.Descending`.
- Add `ITaskletStorage.GetPinnedTaskletsForUserAsync(...)` as an explicit entry point for important pinned tasks.
- Keep `orderBy` for choosing the sortable member.
- Use `sortDirection` for ascending/descending behavior.

Provider behavior:

- Default list ordering should be deterministic:
  - pinned tasks first,
  - then `CreatedAtUtc` descending,
  - then `Id` ascending.
- When `orderBy` is supplied, map the `ISortableTasklet` member expression to a known `Tasklet` property expression.
- Apply pinned-first ordering before the requested sort.
- Always add `Id` as a tie-breaker.

Important implementation detail:

- Do not attempt to pass `Expression<Func<ISortableTasklet, object?>>` directly to `IQueryable<Tasklet>.OrderBy(...)`; the parameter type does not match.
- Parse the member name from the expression and switch over the known sortable members.
- Reject unsupported expressions with a clear exception.

### Pinned Task Entry Point

Pinned Tasklets are important tasks the product should always be able to show in a dedicated lane.

Implementation consequence:

- Add a storage method that returns only `Pinned == true` Tasklets for a user:
  - `GetPinnedTaskletsForUserAsync(userId, skip, take, orderBy, sortDirection)`
- Do not include the free-text `filter` parameter on this method. General search stays on `GetTaskletsForUserAsync(...)`; pinned retrieval is a focused important-task query.
- Add an authenticated API route in Phase 3:
  - `GET /api/v1/tasklets/pinned`
- Return the same `TaskletListResponse` shape as the normal list endpoint so frontend callers can render both lanes with the same response model.
- Keep `GET /api/v1/tasklets` unchanged; it may still include pinned tasks first, but clients that need the important-task lane should call `/tasklets/pinned`.

### API Ownership Enforcement

`GetTaskletByIdAsync(Guid id)` and `DeleteTaskletAsync(Guid id)` are not user-scoped. Handlers must enforce ownership.

Implementation consequence:

- List uses `GetTaskletsForUserAsync(userId, ...)`.
- Pinned list uses `GetPinnedTaskletsForUserAsync(userId, ...)`.
- Get-by-id must return 404 if no task exists or if the task exists but belongs to a different user.
- Update must fetch the existing task first and return 404 if it does not belong to the user.
- Delete must fetch first for ownership, then call delete only when the user owns the task.
- Create must ignore any client-supplied user identity and always set `UserId` from the authenticated `user_id` claim.

### API Route Shape

Expose CRUD under the existing authenticated `/api/v1` group:

- `GET /api/v1/tasklets`
- `GET /api/v1/tasklets/pinned`
- `GET /api/v1/tasklets/{id:guid}`
- `POST /api/v1/tasklets`
- `PUT /api/v1/tasklets/{id:guid}`
- `DELETE /api/v1/tasklets/{id:guid}`

Use `.WithTags("Tasklet")` so Kubb generates a `Tasklet` static client class.

### Checkpoints

Stop after each phase and report:

- Files changed.
- Tests run and results.
- Any behavior differences from this plan.
- Any unresolved issue before the next phase.

The implementation team should not proceed from Phase 1 to Phase 2, or Phase 2 to Phase 3, without an explicit checkpoint.

## Non-Goals

- Do not manually edit generated files in `src/web/src/api/generated/**`.
- Do not build a Tasklet Vue UI in this plan.
- Do not add FTS5, embeddings, summaries, timeline, or heatmap features.
- Do not add a new frontend state store unless Phase 3 is explicitly expanded into UI work.
- Do not stop or restart the whole Aspire AppHost unless `host/Tasklet.AppHost.cs` is changed. This plan should not require AppHost changes.
- Do not weaken Firebase authorization or make endpoints anonymous.
- Do not expose `UserId` as a writable API field.
- Do not make additional storage abstraction changes beyond the approved `SortDirection` parameter and pinned Tasklet entry point unless a phase checkpoint explicitly approves them.

## Phase 1: Sqlite Storage Provider And Schema

### Objective

Implement `SqliteContext` and `SqliteStorageProvider` so the core `ITaskletStorage` contract works against SQLite with EF Core migrations, deterministic query behavior, and provider-level integration tests.

### Files To Update

#### `.gitignore`

Add local SQLite data exclusions:

```gitignore
.data/
*.db-shm
*.db-wal
```

Reason:

- `appsettings.json` writes the local DB under `.data/`.
- SQLite can create `-shm` and `-wal` files when journaling uses WAL mode.

#### `src/backend/core/Model/ITaskletStorage.cs`

Update the list-query contract so sort direction is explicit.

Required change:

```csharp
Task<List<Tasklet>> GetTaskletsForUserAsync(
    string userId,
    int skip = 0,
    int take = 25,
    Expression<Func<ISortableTasklet, object?>>? orderBy = null,
    SortDirection sortDirection = SortDirection.Descending,
    string? filter = null
);

Task<List<Tasklet>> GetPinnedTaskletsForUserAsync(
    string userId,
    int skip = 0,
    int take = 25,
    Expression<Func<ISortableTasklet, object?>>? orderBy = null,
    SortDirection sortDirection = SortDirection.Descending
);
```

Reason:

- API callers should be able to request ascending or descending order without encoding direction into the selected sort field.
- The application needs a separate pinned entry point because important tasks are rendered as their own always-visible lane.

#### `src/backend/sqlite/SqliteContext.cs`

Add EF Core model configuration.

Required changes:

- Add `DbSet<Tasklet.Core.Model.Tasklet> Tasklets`.
- Configure table name `Tasklets`.
- Configure primary key on `Id`.
- Configure `Id` as `ValueGeneratedNever()`.
- Configure `UserId`:
  - required,
  - max length 100,
  - indexed.
- Configure `Title`:
  - required,
  - max length 200.
- Configure `Body`:
  - required,
  - max length 4000.
- Configure enum properties to store as integers:
  - `Status`
  - `Priority`
  - `Color`
- Configure `ExplicitOrder` with a practical max length, recommended 64.
- Configure `CreatedAtUtc`, `CompletedAtUtc`, and `DueAtUtc` as UTC `DateTime` values.
- Add indexes:
  - `(UserId, CreatedAtUtc)`
  - `(UserId, Status)`
  - `(UserId, Priority)`
  - `(UserId, DueAtUtc)`
  - `(UserId, Pinned, ExplicitOrder)`
- Add concise XML comments explaining why mapping lives here instead of the core model.

Notes:

- Keep `Tasklet` in `Tasklet.Core.Model`; do not create a duplicate SQLite entity type unless EF mapping proves impossible.
- Do not add storage annotations to `Tasklet` unless needed; mapping should stay in `SqliteContext`.

#### `src/backend/sqlite/SqliteDesignTimeContextFactory.cs`

Create a design-time context factory for EF migration generation.

Recommended shape:

```csharp
public sealed class SqliteDesignTimeContextFactory : IDesignTimeDbContextFactory<SqliteContext>
{
    public SqliteContext CreateDbContext(string[] args)
        => new(SqliteContext.MakeOptions("Data Source=../../../.data/tasklet-design-time.db"));
}
```

Reason:

- Phase 1 should be able to create migrations before Phase 2 adds the runtime-to-SQLite project reference.
- The design-time database path is only used by tooling to inspect the model; runtime still uses `AppSettings:Storage:ConnectionString`.

#### `src/backend/sqlite/SqliteStorageProvider.cs`

Implement the `ITaskletStorage` contract.

Constructor shape:

```csharp
public partial class SqliteStorageProvider(
    SqliteContext context,
    ILogger<SqliteStorageProvider> logger
) : ITaskletStorage
```

Class requirements:

- Mark class `partial` to support `LoggerMessageAttribute`.
- Use `async` EF Core APIs.
- Use `AsNoTracking()` for read-only queries.
- Use tracked entities for updates where useful.
- Add high-performance logging methods at the bottom of the file.
- Add concise XML comments for public members.

`InitializeAsync()` behavior:

- Read the active SQLite connection string from `context.Database.GetConnectionString()`.
- If the connection string contains a file path, create the parent directory before migration.
- Use `Microsoft.Data.Sqlite.SqliteConnectionStringBuilder` to parse the `Data Source` value instead of hand-parsing the connection string.
- Run `await context.Database.MigrateAsync()`.
- Log the migration/initialization path.

`CreateTaskletAsync(Tasklet tasklet)` behavior:

- If `tasklet.Id == Guid.Empty`, assign `Guid.CreateVersion7()`.
- If `tasklet.CreatedAtUtc == default`, assign `DateTime.UtcNow`.
- Normalize `DateTime` values to UTC where possible:
  - `CreatedAtUtc`
  - `CompletedAtUtc`
  - `DueAtUtc`
- Add and save.
- Return the created entity.

`GetTaskletByIdAsync(Guid id)` behavior:

- Return the tasklet or `null`.
- Use `AsNoTracking()`.

`GetTaskletsForUserAsync(...)` behavior:

- Validate paging:
  - `skip` less than 0 becomes 0.
  - `take` less than or equal to 0 becomes 25.
  - cap `take` at 100.
- Filter by `UserId`.
- If `filter` has non-whitespace text:
  - trim it,
  - escape `%`, `_`, and the escape character before building a `LIKE` pattern,
  - use the `EF.Functions.Like(matchColumn, pattern, escapeCharacter)` overload so the escape behavior is explicit,
  - match against `Title` or `Body`,
  - keep the query provider-translated.
- Sort:
  - pinned first,
  - supplied sortable member if present using `sortDirection`,
  - otherwise `CreatedAtUtc` descending,
  - always add `Id` tie-breaker.
- Return the requested page.

`UpdateTaskletAsync(Tasklet tasklet)` behavior:

- Load the existing tasklet by `Id`.
- If it does not exist, throw a domain-appropriate exception. Recommended: `KeyNotFoundException`.
- Preserve the original `UserId` and `CreatedAtUtc`; do not let an update move a task between users.
- Update mutable fields:
  - `Title`
  - `Body`
  - `Status`
  - `Priority`
  - `ExplicitOrder`
  - `Pinned`
  - `Color`
  - `CompletedAtUtc`
  - `DueAtUtc`
- Save changes.

`DeleteTaskletAsync(Guid id)` behavior:

- Prefer `ExecuteDeleteAsync()` if available and compatible with EF Core 10.
- Return the number of deleted rows.

Helper methods to add:

- `NormalizePaging(int skip, int take)`
- `NormalizeUtc(DateTime value)`
- `NormalizeUtc(DateTime? value)`
- `EscapeLikePattern(string value)`
- `ApplyTaskletOrdering(...)`
- `GetSortableMemberName(Expression<Func<ISortableTasklet, object?>> orderBy)`

#### `src/backend/sqlite/Tasklet.Sqlite.csproj`

Verify required packages remain:

- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Sqlite`

No new package should be needed for Phase 1.

#### `src/backend/sqlite/Migrations/**`

Create the initial migration.

Recommended command:

```shell
dotnet ef migrations add InitialTaskletSchema \
  --project src/backend/sqlite/Tasklet.Sqlite.csproj \
  --output-dir Migrations
```

If `dotnet ef` is not available:

```shell
dotnet tool restore
dotnet ef --version
```

If the tool manifest does not include `dotnet-ef`, stop and ask at the checkpoint before adding tools.

#### `src/tests/Fixtures/SqliteDatabaseFixture.cs`

Update the fixture so it supports migrated temp databases and transaction-aware provider tests.

Required changes:

- Continue using a temp file database path.
- Keep `ConnectionString`.
- Keep `CreateContext()`.
- Replace the singleton `Provider` property with a factory method:

```csharp
public SqliteStorageProvider CreateProvider(SqliteContext context)
```

- In `InitializeAsync()`, create a context and provider, then call `InitializeAsync()` once to migrate the temp DB.
- In `DisposeAsync()`, delete:
  - the main temp DB file,
  - `${dbPath}-shm`,
  - `${dbPath}-wal`.

Reason:

- Provider tests should use the same `SqliteContext` as the transaction base to ensure rollback works.

#### `src/tests/Fixtures/SqliteTransactionalTestBase.cs`

Update this base class so tests can access both the context and provider.

Recommended shape:

```csharp
public abstract class SqliteTransactionalTestBase(SqliteDatabaseFixture fixture)
{
    private IDbContextTransaction? _transaction;

    protected SqliteContext Context { get; private set; } = null!;
    protected SqliteStorageProvider Provider { get; private set; } = null!;

    [Before(Test)]
    public async Task Before()
    {
        Context = fixture.CreateContext();
        Provider = fixture.CreateProvider(Context);
        _transaction = await Context.Database.BeginTransactionAsync();
    }

    [After(Test)]
    public async Task Cleanup()
    {
        ...
    }
}
```

Use PascalCase protected properties instead of the current `_context` field.

#### `src/tests/StorageTests/SqliteStorageProviderTests.cs`

Create storage integration tests.

Test class setup:

- Use a TUnit class fixture for `SqliteDatabaseFixture`.
- Inherit from `SqliteTransactionalTestBase`.
- Keep tests async.
- Await every assertion.
- Include a command comment at the top:

```csharp
/// dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"
```

Required test cases:

1. `InitializeAsync_CreatesSchema_ForTempDatabase`
   - Assert the `Tasklets` table exists after fixture initialization.
2. `CreateTaskletAsync_AssignsVersion7Id_WhenIdIsEmpty`
   - Create with empty `Id`.
   - Assert returned ID is not empty.
   - Assert entity is persisted.
3. `CreateTaskletAsync_PreservesExplicitId_WhenProvided`
   - Create with a supplied `Guid`.
   - Assert returned ID matches.
4. `GetTaskletByIdAsync_ReturnsNull_WhenMissing`
   - Use a random ID.
   - Assert null.
5. `GetTaskletsForUserAsync_ReturnsOnlyRequestedUser`
   - Seed two users.
   - Assert only requested user's tasklets are returned.
6. `GetTaskletsForUserAsync_AppliesSkipTake`
   - Seed at least five rows for one user.
   - Assert page size and expected IDs.
7. `GetTaskletsForUserAsync_CapsTakeAtOneHundred`
   - Seed more than 100 rows or assert by behavior with lower practical count if test runtime is a concern.
8. `GetTaskletsForUserAsync_FiltersTitleAndBody`
   - Seed title-only match, body-only match, and non-match.
   - Assert only matches return.
9. `GetTaskletsForUserAsync_EscapesLikeWildcards`
   - Seed text containing `%` or `_`.
   - Assert literal filter matching does not behave as wildcard matching.
10. `GetTaskletsForUserAsync_SortsByRequestedMemberAndDirection`
    - Use one sortable property such as `Priority` or `DueAtUtc`.
    - Assert ascending and descending orders are deterministic.
11. `GetTaskletsForUserAsync_DefaultSortsPinnedThenNewest`
    - Seed pinned and unpinned tasks with different created times.
    - Assert pinned comes first and newest order is stable.
12. `GetPinnedTaskletsForUserAsync_ReturnsOnlyPinnedForRequestedUser`
    - Seed pinned and unpinned Tasklets for one user and a pinned Tasklet for another user.
    - Assert only the requested user's pinned Tasklet is returned.
13. `GetPinnedTaskletsForUserAsync_AppliesPagingAndSortDirection`
    - Seed multiple pinned Tasklets.
    - Assert paging and explicit sort direction are honored.
14. `UpdateTaskletAsync_PersistsMutableFields`
    - Create, update all mutable fields, read back.
    - Assert updates persisted.
15. `UpdateTaskletAsync_PreservesUserIdAndCreatedAt`
    - Attempt to update with different `UserId` and `CreatedAtUtc`.
    - Assert original values remain.
16. `UpdateTaskletAsync_Throws_WhenMissing`
    - Assert `KeyNotFoundException`.
17. `DeleteTaskletAsync_ReturnsOne_WhenDeleted`
    - Create, delete, assert count 1 and read returns null.
18. `DeleteTaskletAsync_ReturnsZero_WhenMissing`
    - Delete random ID, assert 0.

### Phase 1 Verification

Run formatting:

```shell
dotnet csharpier format src/backend src/tests
```

Run targeted storage tests:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"
```

Run all tests:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo
```

Run build:

```shell
dotnet build Tasklet.slnx --no-restore --nologo
```

Checkpoint before Phase 2:

- Confirm schema migration exists.
- Confirm `.data/` is ignored.
- Confirm storage tests pass.
- Confirm runtime still builds after SQLite project changes.

## Phase 2: Runtime DI Wiring And Startup Initialization

### Objective

Wire SQLite storage into runtime DI through configuration and ensure the database initializes during application startup.

### Files To Update

#### `src/backend/runtime/Tasklet.Runtime.csproj`

Add the SQLite project reference:

```xml
<ProjectReference Include="..\sqlite\Tasklet.Sqlite.csproj" />
```

Reason:

- Runtime is the composition root that chooses the concrete storage provider based on `AppSettings:Storage`.

#### `src/backend/sqlite/SqliteServiceCollectionExtensions.cs`

Create a registration extension in the SQLite project.

Recommended API:

```csharp
public static class SqliteServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTaskletSqliteStorage(StorageSettings storage)
        {
            ...
        }
    }
}
```

Registration behavior:

- Validate `storage.ConnectionString` is not null or whitespace.
- Register `SqliteContext` with `UseSqlite(storage.ConnectionString)`.
- Register `ITaskletStorage` as scoped to `SqliteStorageProvider`.

Comments:

- Include a concise XML comment explaining that this is the SQLite composition boundary.

#### `src/backend/runtime/Config/SetupServicesExtensions.cs`

Add a storage registration method.

Recommended API:

```csharp
public IServiceCollection AddTaskletStorage(AppSettings settings)
```

Behavior:

- Validate `settings.Storage` is present.
- Switch on `settings.Storage.Provider`.
- For `StorageProvider.Sqlite`, call `services.AddTaskletSqliteStorage(settings.Storage)`.
- Throw a clear `InvalidOperationException` for unsupported providers.

Reason:

- Keeps provider selection in runtime configuration and keeps endpoints storage-agnostic.

#### `src/backend/runtime/Program.cs`

Update service registration:

```csharp
builder.Services
    .AddFirebaseAuthentication(settings)
    .AddTaskletHttp(settings)
    .AddTaskletStorage(settings);
```

After `var app = builder.Build();`, initialize storage before `UseTaskletHttp(...)`:

```csharp
await using (var scope = app.Services.CreateAsyncScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<ITaskletStorage>();
    await storage.InitializeAsync();
}
```

Reason:

- The database should be ready before the first request.
- Initialization should run inside a DI scope so scoped `SqliteContext` is valid.

Logging:

- Add a concise startup log before and after storage initialization if an `ILogger` is available.

#### `src/backend/runtime/appsettings.json`

Verify existing settings remain:

```json
"Storage": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=../../../.data/tasklet.db"
}
```

No value change is required unless the implementation team discovers the relative path resolves incorrectly from the runtime working directory.

If path resolution is wrong:

- Prefer changing to `Data Source=.data/tasklet.db` only after checking the process working directory used by Aspire/runtime.
- Update tests or docs with the reason.

### Phase 2 Verification

Run targeted startup/build checks:

```shell
dotnet build src/backend/runtime/Tasklet.Runtime.csproj --no-restore --nologo
```

Run all backend tests:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo
```

Runtime smoke test if Aspire is already running:

- Restart only the `tasklet-api` resource if needed.
- Do not stop the whole Aspire stack.
- Verify health:

```shell
curl http://api.localhost:8089/api/health
```

Expected:

```json
{"status":"Healthy","checkedAt":"..."}
```

Verify database file:

- Confirm `.data/tasklet.db` is created when runtime starts.
- Confirm `git status --porcelain` does not show `.data/tasklet.db`.

Checkpoint before Phase 3:

- Confirm runtime startup initializes storage.
- Confirm `.data/tasklet.db` is ignored.
- Confirm no endpoint code depends on `SqliteContext` directly.

## Phase 3: Tasklet CRUD API Surface

### Objective

Expose authenticated CRUD endpoints for Tasklets using the existing endpoint/handler pattern and verify ownership, validation, OpenAPI generation, and generated frontend client output.

### Files To Create

#### `src/backend/runtime/Endpoints/TaskletCore/TaskletCoreEndpoints.Models.cs`

Create request and response models for the Tasklet API.

Recommended models:

```csharp
public record TaskletResponse(
    Guid Id,
    string UserId,
    string Title,
    string Body,
    Status Status,
    Priority Priority,
    string? ExplicitOrder,
    bool Pinned,
    Color Color,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? DueAtUtc
);

public record TaskletListResponse(IReadOnlyList<TaskletResponse> Items, int Skip, int Take);

public record CreateTaskletRequest(
    string Title,
    string? Body,
    Status? Status,
    Priority? Priority,
    string? ExplicitOrder,
    bool? Pinned,
    Color? Color,
    DateTime? CompletedAtUtc,
    DateTime? DueAtUtc
);

public record UpdateTaskletRequest(
    string Title,
    string? Body,
    Status Status,
    Priority Priority,
    string? ExplicitOrder,
    bool Pinned,
    Color Color,
    DateTime? CompletedAtUtc,
    DateTime? DueAtUtc
);

public enum TaskletSortField
{
    CreatedAtUtc,
    Title,
    Status,
    Priority,
    ExplicitOrder,
    CompletedAtUtc,
    DueAtUtc,
}
```

Model rules:

- Do not allow clients to set `UserId`.
- Do not allow create clients to set `Id`; server assigns it.
- Include `UserId` in responses only if useful for debugging/generated type symmetry. If this feels like unnecessary exposure, remove it before implementation and document the decision in the log.
- Use existing enum types from `Tasklet.Core.Model`.

Mapping helpers:

- Add an internal mapper class or C# 14 extension block in this file or a nearby file:
  - `TaskletResponse ToResponse(this Tasklet tasklet)`
  - `Tasklet ToTasklet(this CreateTaskletRequest request, string userId)`
  - `void ApplyTo(this UpdateTaskletRequest request, Tasklet tasklet)`
  - `Expression<Func<ISortableTasklet, object?>> ToOrderBy(this TaskletSortField sortField)`

Keep mapping code out of the endpoint route mapping class.

#### `src/backend/runtime/Endpoints/TaskletCore/ClaimsPrincipalExtensions.cs`

Create a small internal claims helper if duplicated claim extraction would otherwise appear in multiple handlers.

Recommended behavior:

- Read `user_id` claim.
- Return null when missing or whitespace.
- Optionally read `email` for `GetMeHandler`.

Use C# 14 extension block style if it stays readable:

```csharp
internal static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal? user)
    {
        public string? GetTaskletUserId()
        {
            ...
        }
    }
}
```

If this helper is added, consider updating `GetMeHandler` to use it only if that keeps the change small and tests remain clear.

#### Handler Files

Create one handler class per route, matching the existing runtime convention.

Recommended files:

- `TaskletCoreEndpoints.Handler.List.cs`
- `TaskletCoreEndpoints.Handler.Pinned.cs`
- `TaskletCoreEndpoints.Handler.Get.cs`
- `TaskletCoreEndpoints.Handler.Create.cs`
- `TaskletCoreEndpoints.Handler.Update.cs`
- `TaskletCoreEndpoints.Handler.Delete.cs`

All handlers:

- Implement `IEndpointHandler`.
- Depend on `ITaskletStorage`.
- Accept `ClaimsPrincipal user` in `Handle(...)`.
- Return typed Minimal API results.
- Treat missing `user_id` as `Unauthorized`.
- Add concise XML comments describing route behavior and ownership enforcement.

`ListTaskletsHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `int skip = 0`
  - `int take = 25`
  - `TaskletSortField? sort = null`
  - `SortDirection direction = SortDirection.Descending`
  - `string? filter = null`
- Validate/normalize paging at API boundary, even though storage also guards it.
- Call `GetTaskletsForUserAsync(userId, skip, take, sort.ToOrderBy(), direction, filter)`.
- Return `Ok<TaskletListResponse>`.

`PinnedTaskletsHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `int skip = 0`
  - `int take = 25`
  - `TaskletSortField? sort = null`
  - `SortDirection direction = SortDirection.Descending`
- Validate/normalize paging at API boundary, even though storage also guards it.
- Call `GetPinnedTaskletsForUserAsync(userId, skip, take, sort.ToOrderBy(), direction)`.
- Return `Ok<TaskletListResponse>`.

`GetTaskletHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `Guid id`
- Fetch by ID.
- Return 404 if missing or owned by another user.
- Return `Ok<TaskletResponse>`.

`CreateTaskletHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `CreateTaskletRequest request`
- Validate:
  - title is not null/whitespace,
  - title length <= 200,
  - body length <= 4000,
  - explicit order length <= 64 if present.
- Map to core `Tasklet` with `UserId` from claims.
- Apply defaults:
  - body defaults to empty string,
  - status defaults to `NotStarted`,
  - priority defaults to `Medium`,
  - color defaults to `Lime`,
  - pinned defaults to false.
- Return `Created($"/v1/tasklets/{created.Id}", created.ToResponse())`.

`UpdateTaskletHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `Guid id`
  - `UpdateTaskletRequest request`
- Validate request like create.
- Fetch existing tasklet.
- Return 404 if missing or not owned by user.
- Apply mutable updates.
- Call `UpdateTaskletAsync(existing)`.
- Fetch updated entity or use updated in-memory entity for response.
- Return `Ok<TaskletResponse>`.

`DeleteTaskletHandler`:

- Parameters:
  - `ClaimsPrincipal user`
  - `Guid id`
- Fetch existing tasklet.
- Return 404 if missing or not owned by user.
- Call `DeleteTaskletAsync(id)`.
- Return `NoContent`.

### Files To Update

#### `src/backend/runtime/Endpoints/TaskletCore/TaskletCoreEndpoints.cs`

Map the CRUD routes.

Recommended shape:

```csharp
public void MapEndpoints(IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/tasklets")
        .WithTags("Tasklet");

    group.MapGet("/", (..., [FromServices] ListTaskletsHandler handler) => handler.Handle(...))
        .WithName("ListTasklets")
        .WithDescription("Gets the current user's Tasklets.");

    group.MapGet("/pinned", (..., [FromServices] PinnedTaskletsHandler handler) => handler.Handle(...))
        .WithName("ListPinnedTasklets")
        .WithDescription("Gets the current user's pinned Tasklets.");

    group.MapGet("/{id:guid}", (..., [FromServices] GetTaskletHandler handler) => handler.Handle(...))
        .WithName("GetTasklet")
        .WithDescription("Gets one Tasklet owned by the current user.");

    group.MapPost("/", (..., [FromServices] CreateTaskletHandler handler) => handler.Handle(...))
        .WithName("CreateTasklet")
        .WithDescription("Creates a Tasklet for the current user.");

    group.MapPut("/{id:guid}", (..., [FromServices] UpdateTaskletHandler handler) => handler.Handle(...))
        .WithName("UpdateTasklet")
        .WithDescription("Updates a Tasklet owned by the current user.");

    group.MapDelete("/{id:guid}", (..., [FromServices] DeleteTaskletHandler handler) => handler.Handle(...))
        .WithName("DeleteTasklet")
        .WithDescription("Deletes a Tasklet owned by the current user.");
}
```

Notes:

- The parent `/v1` group already requires authorization.
- Keep route mapping thin; no domain logic here.
- Use `[FromServices]` for handlers like the User endpoint does.

#### `src/tests/EndpointTests/TaskletEndpointTests.cs`

Create handler tests for API logic.

Test setup:

- Use a fake `ITaskletStorage` implementation inside the test file or a small test helper under `src/tests/Fakes`.
- Do not use SQLite for handler tests unless a behavior specifically depends on storage translation.
- Build `ClaimsPrincipal` instances with and without `user_id`.
- Await every assertion.
- Include command comment:

```csharp
/// dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/TaskletEndpointTests/*"
```

Required test cases:

1. List returns unauthorized when no `user_id` claim exists.
2. List calls storage with the authenticated user ID.
3. List passes sort field and sort direction to storage.
4. List returns response items mapped from storage.
5. Pinned list returns unauthorized when no `user_id` claim exists.
6. Pinned list calls storage with the authenticated user ID.
7. Pinned list passes sort field and sort direction to storage.
8. Pinned list returns response items mapped from storage.
9. Get returns unauthorized when no `user_id` claim exists.
10. Get returns not found when storage returns null.
11. Get returns not found when task belongs to a different user.
12. Get returns OK for owned task.
13. Create returns unauthorized when no `user_id` claim exists.
14. Create rejects blank title.
15. Create sets `UserId` from claims and not request body.
16. Create applies defaults for optional fields.
17. Update returns not found for missing task.
18. Update returns not found for task owned by another user.
19. Update preserves `UserId` and `CreatedAtUtc`.
20. Update persists mutable fields.
21. Delete returns not found for missing task.
22. Delete returns not found for task owned by another user.
23. Delete calls storage and returns no content for owned task.

#### `src/web/src/api/tasklet-api.json`

Regenerate only through the backend OpenAPI generation path. Do not edit manually.

Expected changes:

- New `Tasklet` tagged paths.
- New request/response schemas.

#### `src/web/src/api/generated/**`

Regenerate only through Kubb. Do not edit manually.

Expected changes:

- `src/web/src/api/generated/clients/Tasklet.ts`
- Tasklet request/response generated types.
- `src/web/src/api/generated/index.ts` exports for Tasklet client/types.

### Phase 3 Verification

Run targeted endpoint tests:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/TaskletEndpointTests/*"
```

Run storage and endpoint tests together:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/(SqliteStorageProviderTests)|(TaskletEndpointTests)/*"
```

Run all tests:

```shell
dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo
```

Regenerate OpenAPI and frontend clients:

```shell
GEN=true dotnet build src/backend/runtime/Tasklet.Runtime.csproj --no-restore --nologo
```

If the Kubb target does not run from the build, run:

```shell
yarn --cwd src/web generate
```

Verify generated client exists:

```shell
test -f src/web/src/api/generated/clients/Tasklet.ts
```

Verify frontend typecheck/build after generation:

```shell
yarn --cwd src/web build
```

Runtime smoke tests if Aspire is already running:

- Restart only `tasklet-api` if needed.
- Use Firebase emulator to create/sign in a user if exercising authenticated HTTP manually.
- Use generated frontend auth token flow when possible; do not bypass auth in code.
- Verify Scalar shows new Tasklet endpoints at `http://api.localhost:8089/api/scalar`.

Manual HTTP checks require a Firebase bearer token. If manual token acquisition is too slow, rely on handler tests plus one browser login flow through the frontend after API generation.

Playwright:

- Because this plan does not add Tasklet UI screens, Playwright is only required for a login/regression smoke check if the frontend build or generated client integration changes app behavior.
- If implementation expands into Vue Tasklet UI, start by opening Playwright and follow `.agents/context/tasklet-frontend-vue-naiveui.md`.

Checkpoint after Phase 3:

- Confirm all tests pass.
- Confirm OpenAPI schema regenerated.
- Confirm generated Kubb client includes Tasklet endpoints.
- Confirm no generated files were manually edited.
- Confirm runtime smoke checks pass or document why authenticated manual verification was not possible.

## Implementation Order Checklist

### Phase 1 Checklist

- [x] Add `.data/` SQLite ignores.
- [x] Update `ITaskletStorage` to accept `SortDirection`.
- [x] Implement `SqliteContext` `DbSet` and model mapping.
- [x] Add `SqliteDesignTimeContextFactory`.
- [x] Add initial EF migration.
- [x] Implement `SqliteStorageProvider`.
- [x] Update SQLite test fixtures for transaction-aware providers.
- [x] Add `SqliteStorageProviderTests`.
- [ ] Format C# files.
- [x] Run targeted storage tests.
- [x] Run all tests.
- [x] Run solution build.
- [x] Stop for checkpoint.

### Phase 2 Checklist

- [x] Add runtime project reference to `Tasklet.Sqlite`.
- [x] Add SQLite DI extension.
- [x] Add runtime storage registration switch.
- [x] Initialize storage at startup inside a DI scope.
- [x] Verify local `.data/tasklet.db` creation and git ignore behavior.
- [x] Run runtime build.
- [x] Run all tests.
- [x] Run health smoke test if runtime is available.
- [x] Stop for checkpoint.

### Phase 3 Checklist

- [x] Add Tasklet API models and mapping helpers.
- [x] Add claims helper if needed.
- [x] Add list/get/create/update/delete handlers.
- [x] Map Tasklet CRUD routes.
- [x] Add handler tests.
- [x] Run targeted endpoint tests.
- [x] Run storage and endpoint test collection.
- [x] Run all tests.
- [x] Regenerate OpenAPI and Kubb clients.
- [x] Run frontend build/typecheck.
- [x] Run runtime/API smoke checks.
- [x] Stop for final checkpoint.

Note: no standalone formatter command was available or run; generated Kubb output was formatted by its build hook, and C# build/test verification passed.

## Risk Register

### EF Migration Tooling May Be Missing

Risk:

- `dotnet ef` may not be installed in the local tool manifest.

Mitigation:

- Try `dotnet tool restore`.
- If `dotnet ef` is still unavailable, checkpoint before adding tools or using `EnsureCreatedAsync()`.

### Runtime-To-Sqlite Reference Slightly Narrows The Abstraction Boundary

Risk:

- Runtime will reference the SQLite project in the composition root.

Mitigation:

- Keep the reference isolated to configuration/DI code.
- Keep handlers and endpoint logic depending only on `ITaskletStorage`.
- If a future deployment needs provider swapping without runtime references, introduce a separate composition package or plugin loading later.

### Search Is LIKE-Based, Not FTS5

Risk:

- README mentions full-text search as a feature, but this CRUD plan implements simple provider-translated filtering.

Mitigation:

- Document this as a scoped decision in the Phase 1 checkpoint.
- Add FTS5 as a follow-up plan with migrations, virtual table triggers, and ranking tests.

### Ownership Enforcement Depends On Handler Discipline

Risk:

- Storage methods for get/delete by ID are not user-scoped, so a future handler could accidentally expose cross-user data.

Mitigation:

- Add explicit handler tests for cross-user get/update/delete.
- Keep comments near handlers explaining why fetch-before-mutate is required.
- Consider user-scoped storage methods in a future interface revision if more ownership-sensitive operations are added.

### Sort Expression Translation Can Fail At Runtime

Risk:

- `Expression<Func<ISortableTasklet, object?>>` may include boxing or conversion nodes.

Mitigation:

- Add a small parser that unwraps `UnaryExpression` conversions and accepts only `MemberExpression`.
- Test at least one supplied sort expression.
- Throw clear exceptions for unsupported expressions.

### SQLite Relative Path May Resolve Differently Under Aspire

Risk:

- `Data Source=../../../.data/tasklet.db` depends on the runtime process working directory.

Mitigation:

- Verify actual file creation during Phase 2.
- If incorrect, update configuration with the smallest path change and document it in the log.

## Suggested Follow-Up Plans

- FTS5 search with virtual table, triggers, migrations, query ranking, and tests.
- Tasklet Vue screens and Pinia store using generated `Tasklet` Kubb client.
- User profile persistence using the same storage abstraction or a separate profile storage boundary.
- Provider interface revision for user-scoped `GetTaskletForUserAsync(id, userId)` and `DeleteTaskletForUserAsync(id, userId)`.
