using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Tasklet.Endpoints;

/// <summary>
/// Provides telemetry functionality for the Tasklet backend.  Adds some convenience methods.
/// </summary>
internal static class TaskletTelemetry
{
    /// <summary>
    /// The ActivitySource name used for Tasklet backend traces.
    /// </summary>
    internal const string ActivitySourceName = "Tasklet";

    /// <summary>
    /// The ActivitySource for tracing Tasklet backend operations.
    /// See: ee: https://opentelemetry.io/docs/languages/dotnet/traces/best-practices/#activitysource
    /// </summary>
    internal static readonly ActivitySource Tracer = new(ActivitySourceName);

    /// <summary>
    /// The Meter for Tasklet telemetry
    /// See:https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices/#meter
    /// </summary>
    internal static readonly Meter Metrics = new("Tasklet", "1.0");

    /// <summary>
    /// Convenience method to start an activity with just tags.
    /// </summary>
    /// <param name="tags">A set of key-value pairs</param>
    /// <param name="memberName">The call site which automatically sets the activity name.</param>
    /// <param name="filePath">The caller file path added as event metadata.</param>
    /// <param name="lineNumber">The caller line number added as event metadata.</param>
    /// <returns>The new activity.</returns>
    internal static Activity? StartActivity(
        (string Key, object? Value)[] tags,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var parentContext = Activity.Current?.Context ?? default;

        return Tracer.StartActivity(
            memberName,
            kind: ActivityKind.Internal,
            parentContext: parentContext,
            tags:
            [
                .. tags.Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value)),
                new KeyValuePair<string, object?>("code.member", memberName),
                new KeyValuePair<string, object?>("code.file_path", filePath),
                new KeyValuePair<string, object?>("code.line_number", lineNumber),
            ]
        );
    }

    /// <summary>
    /// Adds an event with tuple-style tags to the current activity.
    /// </summary>
    /// <param name="tags">A set of key-value pairs.</param>
    /// <param name="eventName">An optional event name (formulated from the caller member name if not provided).</param>
    /// <param name="name">The caller member name.</param>
    /// <param name="filePath">The caller file path added as event metadata.</param>
    /// <param name="lineNumber">The caller line number added as event metadata.</param>
    /// <returns>The current activity.</returns>
    internal static Activity? AddEvent(
        (string Key, object? Value)[] tags,
        string? eventName = null,
        [CallerMemberName] string name = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    )
    {
        var activity = Activity.Current;

        var effectiveName = eventName ?? $"{name}@{Path.GetFileName(filePath)}:{lineNumber}";

        activity?.AddEvent(
            new ActivityEvent(
                effectiveName,
                tags:
                [
                    .. tags.Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value)),
                    new KeyValuePair<string, object?>("code.member", name),
                    new KeyValuePair<string, object?>("code.file_path", filePath),
                    new KeyValuePair<string, object?>("code.line_number", lineNumber),
                ]
            )
        );

        return activity;
    }
}
