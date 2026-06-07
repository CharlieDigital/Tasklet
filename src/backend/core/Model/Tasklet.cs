using System.ComponentModel.DataAnnotations;

namespace Tasklet.Core.Model;

/// <summary>
/// The main domain model for a Tasklet (a task; we don't use "Task" because this
/// is a key type in .NET so we just use Tasklet to avoid issues with `System.Threading.Task`).
/// This is the core model.  The storage provider must perform the mapping of the
/// fields based on the underlying storage mechanism.  For example, Firestore would
/// not use EF, but Sqlite and Postgres would.
/// </summary>
public class Tasklet : ISortableTasklet
{
    /// <summary>
    /// The unique identifier for the Tasklet. Use UUIDv7 so that
    /// we can sort on this value for better indexing.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the user who owns this Tasklet.
    /// </summary>
    [MaxLength(100)]
    public required string UserId { get; set; }

    /// <summary>
    /// The title of the Tasklet (max 200)
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The body content of the Tasklet.
    /// </summary>
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the Tasklet.
    /// </summary>
    public Status Status { get; set; } = Status.NotStarted;

    /// <summary>
    /// The priority level of the Tasklet.
    /// </summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>
    /// A Lexorank string that represents the explicit order of
    /// the Tasklet in a list.  This is optional and only used
    /// when the user explicitly applies an order.
    /// </summary>
    public string? ExplicitOrder { get; set; }

    /// <summary>
    /// Whether the Tasklet is pinned or not.  Pinned Tasklets
    /// are shown at the top of the list.
    /// </summary>
    public bool Pinned { get; set; } = false;

    /// <summary>
    /// The color assigned to the Tasklet.  The string value will
    /// be combined with an intensity value on the FE like `lime-600`.
    /// This uses the UnoCSS Wind4 presets
    /// See: https://unocss.dev/presets/wind4
    /// See: https://tailwindcss.com/docs/colors
    /// </summary>
    public Color Color { get; set; } = Color.Lime;

    /// <summary>
    /// The timestamp when the Tasklet was created.  This is set
    /// by default to the current time.  We use `DateTime` because
    /// Sqlite does not like `DateTimeOffset` and we want to avoid
    /// issues with timezones, so we'll just store everything in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The timestamp when the Tasklet was last updated.  This is set by default to the current time.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Optional due date for the Tasklet.  User doesn't need to
    /// set this; completely optional.
    /// </summary>
    public DateTime? DueAtUtc { get; set; }
}
