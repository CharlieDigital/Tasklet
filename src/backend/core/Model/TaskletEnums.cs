namespace Tasklet.Core.Model;

/// <summary>
/// Status values for a task.
/// </summary>
public enum Status
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3,
}

/// <summary>
/// Priority values for a task.
/// </summary>
public enum Priority
{
    Eventually = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>
/// Sort direction values for ordered Tasklet queries.
/// </summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1,
}

/// <summary>
/// A color to assign to the task.  On front-end, we can use `lime-600`
/// See: https://tailwindcss.com/docs/colors
/// </summary>
public enum Color
{
    Amber = 0,
    Lime = 1,
    Emerald = 2,
    Cyan = 3,
    Blue = 4,
    Indigo = 5,
    Purple = 6,
    Pink = 7,
    Rose = 8,
}
