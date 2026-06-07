namespace Tasklet.Core.Model;

/// <summary>
/// Interface for the sort expression.
/// </summary>
public interface ISortableTasklet
{
    string Title { get; set; }
    Status Status { get; set; }
    Priority Priority { get; set; }
    string? ExplicitOrder { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime? CompletedAtUtc { get; set; }
    DateTime? DueAtUtc { get; set; }
}
