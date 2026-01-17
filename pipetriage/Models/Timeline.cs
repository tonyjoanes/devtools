namespace PipeTriage.Models;

/// <summary>
/// Immutable record representing a pipeline run timeline.
/// </summary>
public sealed record Timeline(IReadOnlyList<TimelineRecord> Records);

/// <summary>
/// Immutable record representing a timeline record (job/step).
/// </summary>
public sealed record TimelineRecord(
    string Id,
    string Name,
    string Type,
    string Result,
    TimelineLog? Log
)
{
    /// <summary>
    /// Checks if this record represents a failed step.
    /// </summary>
    public bool IsFailed() =>
        Result?.Equals("failed", StringComparison.OrdinalIgnoreCase) ?? false;
}

public sealed record TimelineLog(int Id, string Url);
