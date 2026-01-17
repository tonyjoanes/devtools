namespace PipeTriage.Models;

/// <summary>
/// Immutable record representing pipeline run metadata for output.
/// </summary>
public sealed record Metadata(
    int RunId,
    string RunName,
    string PipelineName,
    string State,
    string Result,
    DateTime? Created,
    DateTime? Finished,
    string Repository,
    string Branch
)
{
    /// <summary>
    /// Creates metadata from RunDetails using pure functional transformation.
    /// </summary>
    public static Metadata FromRunDetails(RunDetails run) =>
        new(
            RunId: run.Id,
            RunName: run.Name,
            PipelineName: run.Pipeline?.Name ?? "Unknown",
            State: run.State,
            Result: run.Result,
            Created: run.CreatedDate,
            Finished: run.FinishedDate,
            Repository: run.Resources?.Repositories?.Self?.Repository?.Name ?? "Unknown",
            Branch: run.Resources?.Repositories?.Self?.RefName ?? "Unknown"
        );
}
