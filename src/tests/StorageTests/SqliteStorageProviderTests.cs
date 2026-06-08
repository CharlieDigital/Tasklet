using Microsoft.EntityFrameworkCore;
using Tasklet.Core.Model;
using Tasklet.Tests.Fixtures;

namespace Tasklet.Tests.StorageTests;

/// <summary>
/// Integration tests for the Sqlite storage provider.
///
/// dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/SqliteStorageProviderTests/*"
/// </summary>
/// <remarks>
/// These tests guard the storage contract at the real provider boundary. They
/// use SQLite instead of mocks because the important behavior includes SQL
/// translation, migration setup, paging, filtering, sorting, and row counts.
/// Handler tests can stay smaller later because this suite proves the provider
/// itself follows the core storage rules.
/// </remarks>
[NotInParallel("SqliteStorage")]
[ClassDataSource<SqliteDatabaseFixture>(Shared = SharedType.PerClass)]
public class SqliteStorageProviderTests(SqliteDatabaseFixture fixture)
    : SqliteTransactionalTestBase(fixture)
{
    [Test]
    public async Task InitializeAsync_CreatesSchema_ForTempDatabase()
    {
        // Guards that provider initialization creates the Tasklets table through
        // the migration path used by production startup.
        var tableCount = await Context
            .Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'Tasklets'"
            )
            .SingleAsync();

        await Assert.That(tableCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateTaskletAsync_AssignsVersion7Id_WhenIdIsEmpty()
    {
        // Guards that new Tasklets without an ID receive a UUIDv7 value before
        // being persisted.
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Created")
        );

        var persisted = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(created.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(created.Id.Version).IsEqualTo(7);
        await Assert.That(persisted).IsNotNull();
    }

    [Test]
    public async Task CreateTaskletAsync_PreservesExplicitId_WhenProvided()
    {
        // Guards that callers can provide an ID and the provider will not
        // replace it during creation.
        var id = Guid.NewGuid();
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(id: id, userId: "user-1", title: "Explicit")
        );

        await Assert.That(created.Id).IsEqualTo(id);
    }

    [Test]
    public async Task GetTaskletByIdAsync_ReturnsNull_WhenMissing()
    {
        // Guards that missing Tasklet lookups return null instead of throwing.
        var tasklet = await Provider.GetTaskletByIdAsync(Guid.NewGuid());

        await Assert.That(tasklet).IsNull();
    }

    [Test]
    public async Task GetTaskletsForUserAsync_ReturnsOnlyRequestedUser()
    {
        // Guards that list queries are scoped to the requested user and do not
        // leak another user's Tasklets.
        await Provider.CreateTaskletAsync(NewTasklet(userId: "user-1", title: "Mine"));
        await Provider.CreateTaskletAsync(NewTasklet(userId: "user-2", title: "Theirs"));

        var results = await Provider.GetTaskletsForUserAsync("user-1");

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].UserId).IsEqualTo("user-1");
    }

    [Test]
    public async Task GetTaskletsForUserAsync_AppliesSkipTake()
    {
        // Guards that list queries apply paging after the deterministic default
        // ordering.
        var first = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "First",
                createdAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var second = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Second",
                createdAtUtc: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var third = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Third",
                createdAtUtc: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var fourth = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Fourth",
                createdAtUtc: new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var fifth = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Fifth",
                createdAtUtc: new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var results = await Provider.GetTaskletsForUserAsync("user-1", skip: 1, take: 2);

        await AssertIdsInOrder(results, fourth.Id, third.Id);

        await Assert
            .That(new[] { first.Id, second.Id, fifth.Id }.Contains(results[0].Id))
            .IsFalse();
    }

    [Test]
    public async Task GetTaskletsForUserAsync_CapsTakeAtOneHundred()
    {
        // Guards that storage bounds oversized pages so callers cannot request
        // unbounded result sets.
        for (var index = 0; index < 105; index++)
        {
            await Provider.CreateTaskletAsync(
                NewTasklet(
                    userId: "user-1",
                    title: $"Tasklet {index}",
                    createdAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(
                        index
                    )
                )
            );
        }

        var results = await Provider.GetTaskletsForUserAsync("user-1", take: 500);

        await Assert.That(results.Count).IsEqualTo(100);
    }

    [Test]
    public async Task GetTaskletsForUserAsync_FiltersTitleAndBody()
    {
        // Guards that the text filter searches both title and body using
        // provider-translated SQL.
        var titleMatch = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Read about lemons")
        );

        var bodyMatch = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Groceries", body: "Buy lemons")
        );

        await Provider.CreateTaskletAsync(NewTasklet(userId: "user-1", title: "Vacuum"));

        var results = await Provider.GetTaskletsForUserAsync("user-1", filter: "lemon");

        await Assert
            .That(results.Select(tasklet => tasklet.Id).ToList())
            .IsEquivalentTo(new[] { titleMatch.Id, bodyMatch.Id });
    }

    [Test]
    public async Task GetTaskletsForUserAsync_EscapesLikeWildcards()
    {
        // Guards that LIKE wildcard characters in user filters are treated as
        // literal text.
        var literalMatch = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Deploy 100% ready")
        );
        await Provider.CreateTaskletAsync(NewTasklet(userId: "user-1", title: "Deploy 1000 ready"));

        var results = await Provider.GetTaskletsForUserAsync("user-1", filter: "100%");

        await Assert
            .That(results.Select(tasklet => tasklet.Id).ToList())
            .IsEquivalentTo(new[] { literalMatch.Id });
    }

    [Test]
    public async Task GetTaskletsForUserAsync_SortsByRequestedMemberAndDirection()
    {
        // Guards that explicit sort field and sort direction are both honored
        // after pinned-first ordering.
        var low = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Low", priority: Priority.Low)
        );

        var high = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "High", priority: Priority.High)
        );

        var medium = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Medium", priority: Priority.Medium)
        );

        var ascending = await Provider.GetTaskletsForUserAsync(
            "user-1",
            orderBy: tasklet => tasklet.Priority,
            sortDirection: SortDirection.Ascending
        );

        var descending = await Provider.GetTaskletsForUserAsync(
            "user-1",
            orderBy: tasklet => tasklet.Priority,
            sortDirection: SortDirection.Descending
        );

        await AssertIdsInOrder(ascending, low.Id, medium.Id, high.Id);
        await AssertIdsInOrder(descending, high.Id, medium.Id, low.Id);
    }

    [Test]
    public async Task GetTaskletsForUserAsync_DefaultSortsPinnedThenNewest()
    {
        // Guards that the default ordering puts pinned Tasklets first, then
        // unpinned Tasklets newest-first.
        var newestUnpinned = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Newest",
                createdAtUtc: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var oldestPinned = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Pinned",
                pinned: true,
                createdAtUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var middleUnpinned = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Middle",
                createdAtUtc: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            )
        );

        var results = await Provider.GetTaskletsForUserAsync("user-1");

        await AssertIdsInOrder(results, oldestPinned.Id, newestUnpinned.Id, middleUnpinned.Id);
    }

    [Test]
    public async Task GetPinnedTaskletsForUserAsync_ReturnsOnlyPinnedForRequestedUser()
    {
        // Guards that the pinned entry point returns only important Tasklets for
        // the requested user.
        var pinned = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Pinned", pinned: true)
        );

        await Provider.CreateTaskletAsync(NewTasklet(userId: "user-1", title: "Unpinned"));
        await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-2", title: "Other user's pinned", pinned: true)
        );

        var results = await Provider.GetPinnedTaskletsForUserAsync("user-1");

        await AssertIdsInOrder(results, pinned.Id);
    }

    [Test]
    public async Task GetPinnedTaskletsForUserAsync_AppliesPagingAndSortDirection()
    {
        // Guards that the pinned entry point uses the same bounded paging and
        // explicit sort direction rules as the normal list query.
        var low = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Low", pinned: true, priority: Priority.Low)
        );

        var high = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "High", pinned: true, priority: Priority.High)
        );

        var medium = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Medium", pinned: true, priority: Priority.Medium)
        );

        var results = await Provider.GetPinnedTaskletsForUserAsync(
            "user-1",
            skip: 1,
            take: 1,
            orderBy: tasklet => tasklet.Priority,
            sortDirection: SortDirection.Descending
        );

        await AssertIdsInOrder(results, medium.Id);
        await Assert.That(new[] { low.Id, high.Id }.Contains(results[0].Id)).IsFalse();
    }

    [Test]
    public async Task GetDoneTaskletsForUserAsync_ReturnsOnlyCompletedForRequestedUser()
    {
        // Guards that the Done tab's storage query is a first-class completed
        // task query rather than a client-side filter over all Tasklets.
        var completed = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Done", status: Status.Completed)
        );

        await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "In progress", status: Status.InProgress)
        );
        await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-2", title: "Other done", status: Status.Completed)
        );

        var results = await Provider.GetDoneTaskletsForUserAsync("user-1");

        await AssertIdsInOrder(results, completed.Id);
    }

    [Test]
    public async Task SetTaskletPinnedAsync_UpdatesOnlyOwnedTasklet()
    {
        // Guards that common pin actions are scoped in storage, so API handlers
        // do not need to load and rewrite the whole Tasklet to flip one flag.
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Pin me")
        );

        var mismatch = await Provider.SetTaskletPinnedAsync(created.Id, "user-2", pinned: true);
        var updated = await Provider.SetTaskletPinnedAsync(created.Id, "user-1", pinned: true);
        var persisted = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(mismatch).IsNull();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Pinned).IsTrue();
        await Assert.That(persisted).IsNotNull();
        await Assert.That(persisted!.Pinned).IsTrue();
    }

    [Test]
    public async Task CompleteTaskletAsync_MarksOwnedTaskletCompleted()
    {
        // Guards that completing a Tasklet updates only completion fields and
        // leaves other mutable fields intact.
        var completedAtUtc = new DateTime(2026, 6, 7, 16, 0, 0, DateTimeKind.Utc);
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(
                userId: "user-1",
                title: "Complete me",
                status: Status.InProgress,
                pinned: true
            )
        );

        var mismatch = await Provider.CompleteTaskletAsync(created.Id, "user-2", completedAtUtc);
        var updated = await Provider.CompleteTaskletAsync(created.Id, "user-1", completedAtUtc);
        var persisted = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(mismatch).IsNull();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Status).IsEqualTo(Status.Completed);
        await Assert.That(updated.CompletedAtUtc).IsEqualTo(completedAtUtc);
        await Assert.That(updated.Pinned).IsTrue();
        await Assert.That(persisted).IsNotNull();
        await Assert.That(persisted!.Status).IsEqualTo(Status.Completed);
        await Assert.That(persisted.CompletedAtUtc).IsEqualTo(completedAtUtc);
    }

    [Test]
    public async Task UpdateTaskletAsync_PersistsMutableFields()
    {
        // Guards that updates persist every mutable Tasklet field.
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Before")
        );

        created.Title = "After";
        created.Body = "Updated body";
        created.Status = Status.Completed;
        created.Priority = Priority.Critical;
        created.ExplicitOrder = "m";
        created.Pinned = true;
        created.Color = Color.Rose;
        created.CompletedAtUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        created.DueAtUtc = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc);

        await Provider.UpdateTaskletAsync(created);

        var updated = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Title).IsEqualTo("After");
        await Assert.That(updated.Body).IsEqualTo("Updated body");
        await Assert.That(updated.Status).IsEqualTo(Status.Completed);
        await Assert.That(updated.Priority).IsEqualTo(Priority.Critical);
        await Assert.That(updated.ExplicitOrder).IsEqualTo("m");
        await Assert.That(updated.Pinned).IsTrue();
        await Assert.That(updated.Color).IsEqualTo(Color.Rose);
        await Assert
            .That(updated.CompletedAtUtc)
            .IsEqualTo(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        await Assert
            .That(updated.DueAtUtc)
            .IsEqualTo(new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task UpdateTaskletAsync_PreservesUserIdAndCreatedAt()
    {
        // Guards that updates cannot move a Tasklet between users or rewrite
        // its original creation timestamp.
        var originalCreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Before", createdAtUtc: originalCreatedAt)
        );

        created.UserId = "user-2";
        created.CreatedAtUtc = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        created.Title = "After";

        await Provider.UpdateTaskletAsync(created);

        var updated = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.UserId).IsEqualTo("user-1");
        await Assert.That(updated.CreatedAtUtc).IsEqualTo(originalCreatedAt);
        await Assert.That(updated.Title).IsEqualTo("After");
    }

    [Test]
    public async Task UpdateTaskletAsync_Throws_WhenMissing()
    {
        // Guards that updating a missing Tasklet fails clearly for callers.
        await Assert
            .That(() =>
                Provider.UpdateTaskletAsync(
                    NewTasklet(id: Guid.NewGuid(), userId: "user-1", title: "Missing")
                )
            )
            .Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task DeleteTaskletAsync_ReturnsOne_WhenDeleted()
    {
        // Guards that deleting an existing Tasklet returns one affected row and
        // removes the row from storage.
        var created = await Provider.CreateTaskletAsync(
            NewTasklet(userId: "user-1", title: "Delete me")
        );

        var deleted = await Provider.DeleteTaskletAsync(created.Id);
        var readBack = await Provider.GetTaskletByIdAsync(created.Id);

        await Assert.That(deleted).IsEqualTo(1);
        await Assert.That(readBack).IsNull();
    }

    [Test]
    public async Task DeleteTaskletAsync_ReturnsZero_WhenMissing()
    {
        // Guards that deleting a missing Tasklet is idempotent from the
        // provider boundary and reports zero affected rows.
        var deleted = await Provider.DeleteTaskletAsync(Guid.NewGuid());

        await Assert.That(deleted).IsEqualTo(0);
    }

    private static Core.Model.Tasklet NewTasklet(
        Guid? id = null,
        string userId = "user-1",
        string title = "Tasklet",
        string body = "",
        Status status = Status.NotStarted,
        Priority priority = Priority.Medium,
        string? explicitOrder = null,
        bool pinned = false,
        Color color = Color.Lime,
        DateTime? createdAtUtc = null,
        DateTime? completedAtUtc = null,
        DateTime? dueAtUtc = null
    ) =>
        new()
        {
            Id = id ?? Guid.Empty,
            UserId = userId,
            Title = title,
            Body = body,
            Status = status,
            Priority = priority,
            ExplicitOrder = explicitOrder,
            Pinned = pinned,
            Color = color,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            CompletedAtUtc = completedAtUtc,
            DueAtUtc = dueAtUtc,
        };

    private static async Task AssertIdsInOrder(
        IReadOnlyList<Core.Model.Tasklet> actual,
        params Guid[] expected
    )
    {
        await Assert.That(actual.Count).IsEqualTo(expected.Length);

        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(actual[index].Id).IsEqualTo(expected[index]);
        }
    }
}
