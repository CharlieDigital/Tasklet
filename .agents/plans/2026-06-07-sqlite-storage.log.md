# Sqlite Storage Provider Work Log

## 2026-06-07

### Phase 1: Sqlite Storage Provider And Schema

- Started Phase 1 from `.agents/plans/2026-06-07-sqlite-storage.working.md`.
- Confirmed working tree was clean after the user's commit and the active branch was `main`.
- Confirmed the only current `GetTaskletsForUserAsync` call sites were the storage interface and the SQLite provider stub before adding the explicit `SortDirection` parameter.
- Confirmed `dotnet ef` is available globally at version `10.0.7`; the local tool manifest does not include `dotnet-ef`.
- Added `.data/`, `*.db-shm`, and `*.db-wal` ignore rules for local SQLite files.
- Updated the storage contract with explicit sort direction and began the provider, EF context, fixture, and storage test implementation.
- Generated `InitialTaskletSchema` via `dotnet ef migrations add`; EF warned that the global tool is `10.0.7` while the runtime package is `10.0.8`.
- First targeted storage test run passed 15/16 tests. The failing preservation test showed created entities remained EF-tracked, allowing later caller mutations to alter tracked original values before `UpdateTaskletAsync`.
- Detached created entities after `SaveChangesAsync()` so provider-returned models behave like detached domain objects and update preservation logic can load the persisted original row.
- Tightened order-sensitive storage tests to assert each result position instead of using unordered equivalence assertions.
- Verification:
  - `dotnet csharpier format src/backend src/tests` failed because `dotnet-csharpier` is not installed on `PATH`.
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"` passed: 16/16 storage tests.
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo` passed: 18/18 tests.
  - `dotnet build Tasklet.slnx --no-restore --nologo` passed with the existing `NETSDK1080` warning for `Tasklet.Core.csproj`.
- Phase 1 checkpoint:
  - Schema migration exists under `src/backend/sqlite/Migrations`.
  - `.data/`, `*.db-shm`, and `*.db-wal` are ignored.
  - Storage tests pass.
  - Runtime and AppHost still build after SQLite project changes.
  - No Aspire restart was performed because Phase 1 did not modify `host/Tasklet.AppHost.cs`.
- Follow-up: added first-line guard comments to every `SqliteStorageProviderTests` test, matching the style in `UserEndpointTests`.
- Follow-up verification:
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"` passed: 16/16 storage tests.
- Follow-up: expanded `/// <remarks>` blocks on the key SQLite provider, context, EF tooling, fixture, transactional base, and storage test classes using plain language about decisions and flow.
- Follow-up verification:
  - `dotnet build src/tests/Tasklet.Tests.csproj --no-restore --nologo` passed with the existing `NETSDK1080` warning.
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"` passed: 16/16 storage tests.
- Follow-up decision: added an explicit pinned Tasklet entry point because pinned tasks are important tasks the product should always be able to show in a dedicated lane.
- Follow-up implementation:
  - Added `ITaskletStorage.GetPinnedTaskletsForUserAsync(...)`.
  - Implemented the pinned query in `SqliteStorageProvider` with user scoping, `Pinned == true`, bounded paging, and explicit sort direction.
  - Added storage tests for pinned-only user scoping and pinned paging/sort behavior.
  - Updated the working plan so Phase 3 includes `GET /api/v1/tasklets/pinned`, a `PinnedTaskletsHandler`, and endpoint tests.
- Follow-up verification:
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"` passed: 18/18 storage tests.
  - `dotnet build src/tests/Tasklet.Tests.csproj --no-restore --nologo` passed with existing warnings in `Tasklet.Core.csproj` and `SetupServicesExtensions.cs`.
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo` passed: 20/20 tests.
