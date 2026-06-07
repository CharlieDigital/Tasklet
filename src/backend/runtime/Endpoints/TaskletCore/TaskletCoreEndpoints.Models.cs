using System.Linq.Expressions;
using Tasklet.Core.Model;

namespace Tasklet.Runtime.Endpoints;

/// <summary>
/// Response model for Tasklet API reads.
/// </summary>
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

/// <summary>
/// Response model for paged Tasklet API lists.
/// </summary>
public record TaskletListResponse(IReadOnlyList<TaskletResponse> Items, int Skip, int Take);

/// <summary>
/// Request model for creating a Tasklet.
/// </summary>
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

/// <summary>
/// Request model for updating a Tasklet.
/// </summary>
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

/// <summary>
/// Sort fields that API callers can request for Tasklet list endpoints.
/// </summary>
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

/// <summary>
/// Maps between Tasklet API models and the core domain model.
/// </summary>
/// <remarks>
/// Handlers should focus on ownership and request flow. This mapper keeps the
/// shape changes in one place, so generated API models can change without
/// spreading conversion code into every handler.
/// </remarks>
internal static class TaskletApiModelMapper
{
    extension(Tasklet.Core.Model.Tasklet tasklet)
    {
        /// <summary>
        /// Converts a core Tasklet to an API response.
        /// </summary>
        public TaskletResponse ToResponse() =>
            new(
                tasklet.Id,
                tasklet.UserId,
                tasklet.Title,
                tasklet.Body,
                tasklet.Status,
                tasklet.Priority,
                tasklet.ExplicitOrder,
                tasklet.Pinned,
                tasklet.Color,
                tasklet.CreatedAtUtc,
                tasklet.CompletedAtUtc,
                tasklet.DueAtUtc
            );
    }

    extension(CreateTaskletRequest request)
    {
        /// <summary>
        /// Converts a create request to a core Tasklet owned by the authenticated user.
        /// </summary>
        public Tasklet.Core.Model.Tasklet ToTasklet(string userId) =>
            new()
            {
                UserId = userId,
                Title = request.Title.Trim(),
                Body = request.Body ?? string.Empty,
                Status = request.Status ?? Status.NotStarted,
                Priority = request.Priority ?? Priority.Medium,
                ExplicitOrder = request.ExplicitOrder,
                Pinned = request.Pinned ?? false,
                Color = request.Color ?? Color.Lime,
                CompletedAtUtc = request.CompletedAtUtc,
                DueAtUtc = request.DueAtUtc,
            };
    }

    extension(UpdateTaskletRequest request)
    {
        /// <summary>
        /// Applies mutable update fields to an existing core Tasklet.
        /// </summary>
        public void ApplyTo(Tasklet.Core.Model.Tasklet tasklet)
        {
            tasklet.Title = request.Title.Trim();
            tasklet.Body = request.Body ?? string.Empty;
            tasklet.Status = request.Status;
            tasklet.Priority = request.Priority;
            tasklet.ExplicitOrder = request.ExplicitOrder;
            tasklet.Pinned = request.Pinned;
            tasklet.Color = request.Color;
            tasklet.CompletedAtUtc = request.CompletedAtUtc;
            tasklet.DueAtUtc = request.DueAtUtc;
        }
    }

    extension(TaskletSortField? sortField)
    {
        /// <summary>
        /// Converts an API sort field to the storage abstraction sort expression.
        /// </summary>
        public Expression<Func<ISortableTasklet, object?>>? ToOrderBy() =>
            sortField switch
            {
                TaskletSortField.CreatedAtUtc => tasklet => tasklet.CreatedAtUtc,
                TaskletSortField.Title => tasklet => tasklet.Title,
                TaskletSortField.Status => tasklet => tasklet.Status,
                TaskletSortField.Priority => tasklet => tasklet.Priority,
                TaskletSortField.ExplicitOrder => tasklet => tasklet.ExplicitOrder,
                TaskletSortField.CompletedAtUtc => tasklet => tasklet.CompletedAtUtc,
                TaskletSortField.DueAtUtc => tasklet => tasklet.DueAtUtc,
                _ => null,
            };
    }
}

/// <summary>
/// Validates Tasklet API requests.
/// </summary>
internal static class TaskletRequestValidation
{
    /// <summary>
    /// Returns a validation error for invalid create request content.
    /// </summary>
    public static string? Validate(CreateTaskletRequest request) =>
        ValidateShared(request.Title, request.Body, request.ExplicitOrder);

    /// <summary>
    /// Returns a validation error for invalid update request content.
    /// </summary>
    public static string? Validate(UpdateTaskletRequest request) =>
        ValidateShared(request.Title, request.Body, request.ExplicitOrder);

    private static string? ValidateShared(string title, string? body, string? explicitOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (title.Length > 200)
        {
            return "Title must be 200 characters or fewer.";
        }

        if (body?.Length > 4000)
        {
            return "Body must be 4000 characters or fewer.";
        }

        if (explicitOrder?.Length > 64)
        {
            return "Explicit order must be 64 characters or fewer.";
        }

        return null;
    }
}
