namespace PipeTriage.Models;

/// <summary>
/// Immutable record representing the complete triage output.
/// </summary>
public sealed record TriageOutput(
    Metadata Metadata,
    string PipelineYaml,
    FailedStepInfo? FailedStep
)
{
    public bool HasFailedStep => FailedStep is not null;
}

/// <summary>
/// Immutable record representing information about a failed step.
/// </summary>
public sealed record FailedStepInfo(
    string StepName,
    string LogContent
);

/// <summary>
/// Immutable record representing triage request parameters.
/// </summary>
public sealed record TriageRequest(
    string Organization,
    string Project,
    int RunId,
    string OutputDirectory
);
