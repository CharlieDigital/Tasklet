using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Tasklet.Core.Model;
using Tasklet.Runtime.Endpoints;
using CoreTasklet = Tasklet.Core.Model.Tasklet;

namespace Tasklet.Tests.EndpointTests;

/// <summary>
/// Test cases for the Tasklet endpoints.
///
/// dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo --treenode-filter "/*/*/TaskletEndpointTests/*"
/// </summary>
/// <remarks>
/// These tests guard the thin API layer. Storage tests cover SQL behavior, so
/// this suite focuses on claims, ownership checks, request validation, and the
/// calls each handler makes into the storage boundary.
/// </remarks>
public class TaskletEndpointTests
{
    [Test]
    public async Task List_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Guards that list queries require the Firebase user_id claim before
        // storage is touched.
        var storage = new FakeTaskletStorage();
        var handler = new ListTaskletsHandler(storage);

        var result = await handler.Handle(new ClaimsPrincipal());

        await Assert.That(result.Result).IsOfType(typeof(UnauthorizedHttpResult));
        await Assert.That(storage.ListCalled).IsFalse();
    }

    [Test]
    public async Task List_CallsStorageWithAuthenticatedUserId()
    {
        // Guards that list queries are scoped to the authenticated Firebase
        // user ID instead of accepting ownership from the request.
        var storage = new FakeTaskletStorage();
        var handler = new ListTaskletsHandler(storage);

        var result = await handler.Handle(User("user-1"));

        await Assert.That(result.Result).IsOfType(typeof(Ok<TaskletListResponse>));
        await Assert.That(storage.LastUserId).IsEqualTo("user-1");
    }

    [Test]
    public async Task List_PassesPagingSortDirectionAndFilterToStorage()
    {
        // Guards that list query knobs are normalized and forwarded to the
        // storage boundary.
        var storage = new FakeTaskletStorage();
        var handler = new ListTaskletsHandler(storage);

        await handler.Handle(
            User("user-1"),
            skip: -5,
            take: 500,
            sort: TaskletSortField.Priority,
            direction: SortDirection.Ascending,
            filter: "urgent"
        );

        await Assert.That(storage.LastSkip).IsEqualTo(0);
        await Assert.That(storage.LastTake).IsEqualTo(100);
        await Assert.That(storage.LastSortDirection).IsEqualTo(SortDirection.Ascending);
        await Assert.That(storage.LastFilter).IsEqualTo("urgent");
        await Assert
            .That(SortedMemberName(storage.LastOrderBy))
            .IsEqualTo(nameof(ISortableTasklet.Priority));
    }

    [Test]
    public async Task List_MapsStorageTaskletsToResponse()
    {
        // Guards that list responses expose the Tasklet fields returned by
        // storage without changing their meaning.
        var tasklet = NewTasklet(
            userId: "user-1",
            title: "Mapped",
            body: "Body",
            status: Status.InProgress,
            priority: Priority.High,
            explicitOrder: "n",
            pinned: true,
            color: Color.Cyan,
            completedAtUtc: new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            dueAtUtc: new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc)
        );
        var storage = new FakeTaskletStorage { ListResult = [tasklet] };
        var handler = new ListTaskletsHandler(storage);

        var result = await handler.Handle(User("user-1"));
        var ok = result.Result as Ok<TaskletListResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsNotNull();
        await Assert.That(ok.Value!.Items.Count).IsEqualTo(1);
        await Assert.That(ok.Value.Items[0].Id).IsEqualTo(tasklet.Id);
        await Assert.That(ok.Value.Items[0].UserId).IsEqualTo("user-1");
        await Assert.That(ok.Value.Items[0].Title).IsEqualTo("Mapped");
        await Assert.That(ok.Value.Items[0].Body).IsEqualTo("Body");
        await Assert.That(ok.Value.Items[0].Status).IsEqualTo(Status.InProgress);
        await Assert.That(ok.Value.Items[0].Priority).IsEqualTo(Priority.High);
        await Assert.That(ok.Value.Items[0].ExplicitOrder).IsEqualTo("n");
        await Assert.That(ok.Value.Items[0].Pinned).IsTrue();
        await Assert.That(ok.Value.Items[0].Color).IsEqualTo(Color.Cyan);
    }

    [Test]
    public async Task Pinned_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Guards that pinned queries require the Firebase user_id claim before
        // storage is touched.
        var storage = new FakeTaskletStorage();
        var handler = new PinnedTaskletsHandler(storage);

        var result = await handler.Handle(new ClaimsPrincipal());

        await Assert.That(result.Result).IsOfType(typeof(UnauthorizedHttpResult));
        await Assert.That(storage.PinnedListCalled).IsFalse();
    }

    [Test]
    public async Task Pinned_UsesExplicitPinnedStorageEntryPoint()
    {
        // Guards that the pinned API calls the dedicated important-task query
        // instead of reusing the general list endpoint with a hidden filter.
        var storage = new FakeTaskletStorage();
        var handler = new PinnedTaskletsHandler(storage);

        var result = await handler.Handle(User("user-1"));

        await Assert.That(result.Result).IsOfType(typeof(Ok<TaskletListResponse>));
        await Assert.That(storage.PinnedListCalled).IsTrue();
        await Assert.That(storage.ListCalled).IsFalse();
        await Assert.That(storage.LastUserId).IsEqualTo("user-1");
    }

    [Test]
    public async Task Pinned_PassesPagingSortAndDirectionToStorage()
    {
        // Guards that the pinned API supports the same bounded paging and sort
        // direction behavior as the main list API.
        var storage = new FakeTaskletStorage();
        var handler = new PinnedTaskletsHandler(storage);

        await handler.Handle(
            User("user-1"),
            skip: 2,
            take: 0,
            sort: TaskletSortField.DueAtUtc,
            direction: SortDirection.Ascending
        );

        await Assert.That(storage.LastSkip).IsEqualTo(2);
        await Assert.That(storage.LastTake).IsEqualTo(25);
        await Assert.That(storage.LastSortDirection).IsEqualTo(SortDirection.Ascending);
        await Assert
            .That(SortedMemberName(storage.LastOrderBy))
            .IsEqualTo(nameof(ISortableTasklet.DueAtUtc));
    }

    [Test]
    public async Task Pinned_MapsStorageTaskletsToResponse()
    {
        // Guards that pinned responses use the same response model as the main
        // list so the frontend has one Tasklet shape to consume.
        var tasklet = NewTasklet(userId: "user-1", title: "Pinned", pinned: true);
        var storage = new FakeTaskletStorage { PinnedListResult = [tasklet] };
        var handler = new PinnedTaskletsHandler(storage);

        var result = await handler.Handle(User("user-1"));
        var ok = result.Result as Ok<TaskletListResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsNotNull();
        await Assert.That(ok.Value!.Items.Count).IsEqualTo(1);
        await Assert.That(ok.Value.Items[0].Id).IsEqualTo(tasklet.Id);
        await Assert.That(ok.Value.Items[0].Pinned).IsTrue();
    }

    [Test]
    public async Task Done_UsesExplicitDoneStorageEntryPoint()
    {
        // Guards that the Done API calls the dedicated completed-task query the
        // frontend Done tab will consume.
        var tasklet = NewTasklet(userId: "user-1", title: "Done", status: Status.Completed);
        var storage = new FakeTaskletStorage { DoneListResult = [tasklet] };
        var handler = new DoneTaskletsHandler(storage);

        var result = await handler.Handle(User("user-1"));
        var ok = result.Result as Ok<TaskletListResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(storage.DoneListCalled).IsTrue();
        await Assert.That(storage.LastUserId).IsEqualTo("user-1");
        await Assert.That(ok!.Value).IsNotNull();
        await Assert.That(ok.Value!.Items[0].Status).IsEqualTo(Status.Completed);
    }

    [Test]
    public async Task Get_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Guards that single Tasklet reads require authentication before
        // loading any row by ID.
        var storage = new FakeTaskletStorage();
        var handler = new GetTaskletHandler(storage);

        var result = await handler.Handle(new ClaimsPrincipal(), Guid.NewGuid());

        await Assert.That(result.Result).IsOfType(typeof(UnauthorizedHttpResult));
        await Assert.That(storage.GetByIdCalled).IsFalse();
    }

    [Test]
    public async Task Get_ReturnsNotFound_WhenTaskletIsMissing()
    {
        // Guards that missing Tasklet reads return 404 instead of leaking
        // storage details.
        var storage = new FakeTaskletStorage();
        var handler = new GetTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), Guid.NewGuid());

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
    }

    [Test]
    public async Task Get_ReturnsNotFound_WhenTaskletBelongsToAnotherUser()
    {
        // Guards that reads do not reveal another user's Tasklet even when the
        // caller knows its ID.
        var storage = new FakeTaskletStorage { TaskletByIdResult = NewTasklet(userId: "user-2") };
        var handler = new GetTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), storage.TaskletByIdResult.Id);

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
    }

    [Test]
    public async Task Get_ReturnsTasklet_WhenOwnedByAuthenticatedUser()
    {
        // Guards that owned Tasklets can be read through the API response
        // mapper.
        var tasklet = NewTasklet(userId: "user-1", title: "Owned");
        var storage = new FakeTaskletStorage { TaskletByIdResult = tasklet };
        var handler = new GetTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), tasklet.Id);
        var ok = result.Result as Ok<TaskletResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value).IsNotNull();
        await Assert.That(ok.Value!.Id).IsEqualTo(tasklet.Id);
        await Assert.That(ok.Value.Title).IsEqualTo("Owned");
    }

    [Test]
    public async Task Create_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        // Guards that creates require authentication before the request is
        // mapped into a Tasklet.
        var storage = new FakeTaskletStorage();
        var handler = new CreateTaskletHandler(storage);

        var result = await handler.Handle(new ClaimsPrincipal(), CreateRequest(title: "New"));

        await Assert.That(result.Result).IsOfType(typeof(UnauthorizedHttpResult));
        await Assert.That(storage.CreatedTasklet).IsNull();
    }

    [Test]
    public async Task Create_ReturnsBadRequest_WhenTitleIsBlank()
    {
        // Guards that creates reject blank titles before writing to storage.
        var storage = new FakeTaskletStorage();
        var handler = new CreateTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), CreateRequest(title: " "));

        await Assert.That(result.Result).IsOfType(typeof(BadRequest<string>));
        await Assert.That(storage.CreatedTasklet).IsNull();
    }

    [Test]
    public async Task Create_AssignsUserIdFromClaims()
    {
        // Guards that Tasklet ownership comes from Firebase claims, not from
        // client-controlled request content.
        var storage = new FakeTaskletStorage();
        var handler = new CreateTaskletHandler(storage);

        await handler.Handle(User("user-1"), CreateRequest(title: "Mine"));

        await Assert.That(storage.CreatedTasklet).IsNotNull();
        await Assert.That(storage.CreatedTasklet!.UserId).IsEqualTo("user-1");
    }

    [Test]
    public async Task Create_AppliesDefaultsForOptionalFields()
    {
        // Guards that omitted create fields use the domain defaults the UI can
        // rely on after a quick-add flow.
        var storage = new FakeTaskletStorage();
        var handler = new CreateTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), CreateRequest(title: "Defaults"));
        var created = result.Result as Created<TaskletResponse>;

        await Assert.That(created).IsNotNull();
        await Assert.That(storage.CreatedTasklet).IsNotNull();
        await Assert.That(storage.CreatedTasklet!.Body).IsEqualTo(string.Empty);
        await Assert.That(storage.CreatedTasklet.Status).IsEqualTo(Status.NotStarted);
        await Assert.That(storage.CreatedTasklet.Priority).IsEqualTo(Priority.Medium);
        await Assert.That(storage.CreatedTasklet.Pinned).IsFalse();
        await Assert.That(storage.CreatedTasklet.Color).IsEqualTo(Color.Lime);
        await Assert.That(created!.Value).IsNotNull();
        await Assert.That(created.Value!.Status).IsEqualTo(Status.NotStarted);
    }

    [Test]
    public async Task Update_ReturnsNotFound_WhenTaskletIsMissing()
    {
        // Guards that updating a missing Tasklet returns 404 with a valid
        // request body.
        var storage = new FakeTaskletStorage();
        var handler = new UpdateTaskletHandler(storage);

        var result = await handler.Handle(
            User("user-1"),
            Guid.NewGuid(),
            UpdateRequest(title: "After")
        );

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
        await Assert.That(storage.UpdatedTasklet).IsNull();
    }

    [Test]
    public async Task Update_ReturnsNotFound_WhenTaskletBelongsToAnotherUser()
    {
        // Guards that updates cannot change another user's Tasklet even when
        // the caller knows its ID.
        var storage = new FakeTaskletStorage { TaskletByIdResult = NewTasklet(userId: "user-2") };
        var handler = new UpdateTaskletHandler(storage);

        var result = await handler.Handle(
            User("user-1"),
            storage.TaskletByIdResult.Id,
            UpdateRequest(title: "After")
        );

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
        await Assert.That(storage.UpdatedTasklet).IsNull();
    }

    [Test]
    public async Task Update_PreservesUserIdAndCreatedAt()
    {
        // Guards that API updates only mutate editable fields and leave
        // ownership plus creation time alone.
        var createdAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = NewTasklet(userId: "user-1", title: "Before", createdAtUtc: createdAtUtc);
        var storage = new FakeTaskletStorage { TaskletByIdResult = existing };
        var handler = new UpdateTaskletHandler(storage);

        await handler.Handle(User("user-1"), existing.Id, UpdateRequest(title: "After"));

        await Assert.That(storage.UpdatedTasklet).IsNotNull();
        await Assert.That(storage.UpdatedTasklet!.UserId).IsEqualTo("user-1");
        await Assert.That(storage.UpdatedTasklet.CreatedAtUtc).IsEqualTo(createdAtUtc);
    }

    [Test]
    public async Task Update_PersistsMutableFields()
    {
        // Guards that every mutable Tasklet field accepted by the API is sent
        // to storage.
        var completedAtUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var dueAtUtc = new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc);
        var existing = NewTasklet(userId: "user-1", title: "Before");
        var storage = new FakeTaskletStorage { TaskletByIdResult = existing };
        var handler = new UpdateTaskletHandler(storage);

        var result = await handler.Handle(
            User("user-1"),
            existing.Id,
            UpdateRequest(
                title: "After",
                body: "Body",
                status: Status.Completed,
                priority: Priority.Critical,
                explicitOrder: "m",
                pinned: true,
                color: Color.Rose,
                completedAtUtc: completedAtUtc,
                dueAtUtc: dueAtUtc
            )
        );
        var ok = result.Result as Ok<TaskletResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(storage.UpdatedTasklet).IsNotNull();
        await Assert.That(storage.UpdatedTasklet!.Title).IsEqualTo("After");
        await Assert.That(storage.UpdatedTasklet.Body).IsEqualTo("Body");
        await Assert.That(storage.UpdatedTasklet.Status).IsEqualTo(Status.Completed);
        await Assert.That(storage.UpdatedTasklet.Priority).IsEqualTo(Priority.Critical);
        await Assert.That(storage.UpdatedTasklet.ExplicitOrder).IsEqualTo("m");
        await Assert.That(storage.UpdatedTasklet.Pinned).IsTrue();
        await Assert.That(storage.UpdatedTasklet.Color).IsEqualTo(Color.Rose);
        await Assert.That(storage.UpdatedTasklet.CompletedAtUtc).IsEqualTo(completedAtUtc);
        await Assert.That(storage.UpdatedTasklet.DueAtUtc).IsEqualTo(dueAtUtc);
    }

    [Test]
    public async Task Pin_CallsScopedStorageAndReturnsUpdatedTasklet()
    {
        // Guards that pinning uses the direct storage entry point rather than
        // requiring clients to send a full update payload.
        var tasklet = NewTasklet(userId: "user-1", title: "Pin me");
        var storage = new FakeTaskletStorage { SetPinnedResult = tasklet };
        var handler = new PinTaskletHandler(storage);

        var result = await handler.Pin(User("user-1"), tasklet.Id);
        var ok = result.Result as Ok<TaskletResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(storage.LastPinnedId).IsEqualTo(tasklet.Id);
        await Assert.That(storage.LastPinnedUserId).IsEqualTo("user-1");
        await Assert.That(storage.LastPinnedValue).IsTrue();
    }

    [Test]
    public async Task Unpin_CallsScopedStorageAndReturnsUpdatedTasklet()
    {
        // Guards that unpinning uses the same direct storage entry point with
        // an explicit false value.
        var tasklet = NewTasklet(userId: "user-1", title: "Unpin me", pinned: true);
        var storage = new FakeTaskletStorage { SetPinnedResult = tasklet };
        var handler = new PinTaskletHandler(storage);

        var result = await handler.Unpin(User("user-1"), tasklet.Id);
        var ok = result.Result as Ok<TaskletResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(storage.LastPinnedId).IsEqualTo(tasklet.Id);
        await Assert.That(storage.LastPinnedUserId).IsEqualTo("user-1");
        await Assert.That(storage.LastPinnedValue).IsFalse();
    }

    [Test]
    public async Task Pin_ReturnsNotFound_WhenStorageFindsNoOwnedTasklet()
    {
        var storage = new FakeTaskletStorage();
        var handler = new PinTaskletHandler(storage);

        var result = await handler.Pin(User("user-1"), Guid.NewGuid());

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
    }

    [Test]
    public async Task Complete_CallsScopedStorageAndReturnsUpdatedTasklet()
    {
        // Guards that completion has a direct endpoint which sets completion
        // server-side without clients posting every mutable Tasklet field.
        var tasklet = NewTasklet(
            userId: "user-1",
            title: "Complete me",
            status: Status.Completed,
            completedAtUtc: DateTime.UtcNow
        );
        var storage = new FakeTaskletStorage { CompleteResult = tasklet };
        var handler = new CompleteTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), tasklet.Id);
        var ok = result.Result as Ok<TaskletResponse>;

        await Assert.That(ok).IsNotNull();
        await Assert.That(storage.LastCompleteId).IsEqualTo(tasklet.Id);
        await Assert.That(storage.LastCompleteUserId).IsEqualTo("user-1");
        await Assert.That(storage.LastCompleteAtUtc).IsNotNull();
    }

    [Test]
    public async Task Complete_ReturnsNotFound_WhenStorageFindsNoOwnedTasklet()
    {
        var storage = new FakeTaskletStorage();
        var handler = new CompleteTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), Guid.NewGuid());

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
    }

    [Test]
    public async Task Delete_ReturnsNotFound_WhenTaskletIsMissing()
    {
        // Guards that deleting a missing Tasklet returns 404 instead of
        // treating it as an owned delete.
        var storage = new FakeTaskletStorage();
        var handler = new DeleteTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), Guid.NewGuid());

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
        await Assert.That(storage.DeletedId).IsNull();
    }

    [Test]
    public async Task Delete_ReturnsNotFound_WhenTaskletBelongsToAnotherUser()
    {
        // Guards that deletes do not remove another user's Tasklet even when
        // the caller knows its ID.
        var storage = new FakeTaskletStorage { TaskletByIdResult = NewTasklet(userId: "user-2") };
        var handler = new DeleteTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), storage.TaskletByIdResult.Id);

        await Assert.That(result.Result).IsOfType(typeof(NotFound));
        await Assert.That(storage.DeletedId).IsNull();
    }

    [Test]
    public async Task Delete_CallsStorageWhenTaskletIsOwned()
    {
        // Guards that owned deletes call storage and return the API no-content
        // result expected by clients.
        var tasklet = NewTasklet(userId: "user-1");
        var storage = new FakeTaskletStorage { TaskletByIdResult = tasklet };
        var handler = new DeleteTaskletHandler(storage);

        var result = await handler.Handle(User("user-1"), tasklet.Id);

        await Assert.That(result.Result).IsOfType(typeof(NoContent));
        await Assert.That(storage.DeletedId).IsEqualTo(tasklet.Id);
    }

    private static ClaimsPrincipal User(string userId)
    {
        var identity = new ClaimsIdentity([new Claim("user_id", userId)], "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    private static CreateTaskletRequest CreateRequest(
        string title,
        string? body = null,
        Status? status = null,
        Priority? priority = null,
        string? explicitOrder = null,
        bool? pinned = null,
        Color? color = null,
        DateTime? completedAtUtc = null,
        DateTime? dueAtUtc = null
    ) => new(title, body, status, priority, explicitOrder, pinned, color, completedAtUtc, dueAtUtc);

    private static UpdateTaskletRequest UpdateRequest(
        string title,
        string? body = null,
        Status status = Status.InProgress,
        Priority priority = Priority.High,
        string? explicitOrder = null,
        bool pinned = false,
        Color color = Color.Blue,
        DateTime? completedAtUtc = null,
        DateTime? dueAtUtc = null
    ) => new(title, body, status, priority, explicitOrder, pinned, color, completedAtUtc, dueAtUtc);

    private static CoreTasklet NewTasklet(
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
            Id = id ?? Guid.NewGuid(),
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

    private static string? SortedMemberName(Expression<Func<ISortableTasklet, object?>>? orderBy) =>
        orderBy?.Body switch
        {
            MemberExpression memberExpression => memberExpression.Member.Name,
            UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression
                .Member
                .Name,
            _ => null,
        };

    private sealed class FakeTaskletStorage : ITaskletStorage
    {
        public bool ListCalled { get; private set; }
        public bool PinnedListCalled { get; private set; }
        public bool DoneListCalled { get; private set; }
        public bool GetByIdCalled { get; private set; }
        public string? LastUserId { get; private set; }
        public int? LastSkip { get; private set; }
        public int? LastTake { get; private set; }
        public SortDirection? LastSortDirection { get; private set; }
        public string? LastFilter { get; private set; }
        public Expression<Func<ISortableTasklet, object?>>? LastOrderBy { get; private set; }
        public List<CoreTasklet> ListResult { get; init; } = [];
        public List<CoreTasklet> PinnedListResult { get; init; } = [];
        public List<CoreTasklet> DoneListResult { get; init; } = [];
        public CoreTasklet? TaskletByIdResult { get; init; }
        public CoreTasklet? CreatedTasklet { get; private set; }
        public CoreTasklet? UpdatedTasklet { get; private set; }
        public Guid? DeletedId { get; private set; }
        public Guid? LastPinnedId { get; private set; }
        public string? LastPinnedUserId { get; private set; }
        public bool? LastPinnedValue { get; private set; }
        public CoreTasklet? SetPinnedResult { get; init; }
        public Guid? LastCompleteId { get; private set; }
        public string? LastCompleteUserId { get; private set; }
        public DateTime? LastCompleteAtUtc { get; private set; }
        public CoreTasklet? CompleteResult { get; init; }

        public Task<List<CoreTasklet>> GetTaskletsForUserAsync(
            string userId,
            int skip = 0,
            int take = 25,
            Expression<Func<ISortableTasklet, object?>>? orderBy = null,
            SortDirection sortDirection = SortDirection.Descending,
            string? filter = null
        )
        {
            ListCalled = true;
            RecordQuery(userId, skip, take, orderBy, sortDirection, filter);
            return Task.FromResult(ListResult);
        }

        public Task<List<CoreTasklet>> GetPinnedTaskletsForUserAsync(
            string userId,
            int skip = 0,
            int take = 25,
            Expression<Func<ISortableTasklet, object?>>? orderBy = null,
            SortDirection sortDirection = SortDirection.Descending
        )
        {
            PinnedListCalled = true;
            RecordQuery(userId, skip, take, orderBy, sortDirection, filter: null);
            return Task.FromResult(PinnedListResult);
        }

        public Task<List<CoreTasklet>> GetDoneTaskletsForUserAsync(
            string userId,
            int skip = 0,
            int take = 25,
            Expression<Func<ISortableTasklet, object?>>? orderBy = null,
            SortDirection sortDirection = SortDirection.Descending
        )
        {
            DoneListCalled = true;
            RecordQuery(userId, skip, take, orderBy, sortDirection, filter: null);
            return Task.FromResult(DoneListResult);
        }

        public Task<CoreTasklet?> GetTaskletByIdAsync(Guid id)
        {
            GetByIdCalled = true;
            return Task.FromResult(TaskletByIdResult);
        }

        public Task<CoreTasklet> CreateTaskletAsync(CoreTasklet tasklet)
        {
            CreatedTasklet = tasklet;

            if (CreatedTasklet.Id == Guid.Empty)
            {
                CreatedTasklet.Id = Guid.NewGuid();
            }

            return Task.FromResult(CreatedTasklet);
        }

        public Task UpdateTaskletAsync(CoreTasklet tasklet)
        {
            UpdatedTasklet = tasklet;
            return Task.CompletedTask;
        }

        public Task<CoreTasklet?> SetTaskletPinnedAsync(Guid id, string userId, bool pinned)
        {
            LastPinnedId = id;
            LastPinnedUserId = userId;
            LastPinnedValue = pinned;
            return Task.FromResult(SetPinnedResult);
        }

        public Task<CoreTasklet?> CompleteTaskletAsync(
            Guid id,
            string userId,
            DateTime completedAtUtc
        )
        {
            LastCompleteId = id;
            LastCompleteUserId = userId;
            LastCompleteAtUtc = completedAtUtc;
            return Task.FromResult(CompleteResult);
        }

        public Task<int> DeleteTaskletAsync(Guid id)
        {
            DeletedId = id;
            return Task.FromResult(1);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        private void RecordQuery(
            string userId,
            int skip,
            int take,
            Expression<Func<ISortableTasklet, object?>>? orderBy,
            SortDirection sortDirection,
            string? filter
        )
        {
            LastUserId = userId;
            LastSkip = skip;
            LastTake = take;
            LastOrderBy = orderBy;
            LastSortDirection = sortDirection;
            LastFilter = filter;
        }
    }
}
