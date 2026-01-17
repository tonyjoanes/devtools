using PipeTriage.Models;

namespace PipeTriage.Core;

/// <summary>
/// Interface for Azure DevOps API client operations.
/// </summary>
public interface IAzureDevOpsClient
{
    /// <summary>
    /// Fetches run details from Azure DevOps.
    /// </summary>
    Task<Result<RunDetails>> GetRunAsync(string org, string project, int runId);

    /// <summary>
    /// Fetches pipeline YAML definition.
    /// </summary>
    Task<Result<string>> GetPipelineYamlAsync(string org, string project, int runId);

    /// <summary>
    /// Fetches timeline data for a run.
    /// </summary>
    Task<Result<Timeline>> GetTimelineAsync(string org, string project, int runId);

    /// <summary>
    /// Downloads log content from a URL.
    /// </summary>
    Task<Result<string>> GetLogContentAsync(string logUrl);
}
