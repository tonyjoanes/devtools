using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PipeTriage.Core;
using PipeTriage.Models;
using static PipeTriage.Core.ResultExtensions;

namespace PipeTriage;

/// <summary>
/// Functional Azure DevOps client using Result types for error handling.
/// </summary>
public sealed class AzureDevOpsClient : IAzureDevOpsClient
{
    private readonly HttpClient _httpClient;
    private const string ApiVersion = "7.1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AzureDevOpsClient(HttpClient httpClient, string pat)
    {
        _httpClient = httpClient;
        ConfigureHttpClient(pat);
    }

    private void ConfigureHttpClient(string pat)
    {
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authHeader);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Get run details using Pipelines Runs API (functional approach with Result).
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/get
    /// </summary>
    public Task<Result<RunDetails>> GetRunAsync(string org, string project, int runId) =>
        TryAsync(async () =>
        {
            var url = BuildRunUrl(org, project, runId);
            var json = await FetchJsonAsync(url);
            return DeserializeJson<RunDetails>(json);
        });

    /// <summary>
    /// Get pipeline YAML using Build YAML API (functional approach with Result).
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/build/yaml/get
    /// </summary>
    public async Task<Result<string>> GetPipelineYamlAsync(string org, string project, int runId)
    {
        var url = BuildYamlUrl(org, project, runId);

        // Try to fetch YAML, fallback to placeholder on failure
        var result = await TryAsync(() => FetchTextAsync(url));

        return result.Match(
            onSuccess: yaml => Result<string>.Ok(yaml),
            onFailure: _ =>
            {
                // Use run info for fallback message if available
                var runResult = GetRunAsync(org, project, runId).Result;
                var pipelineName = runResult.Match(
                    onSuccess: run => run.Pipeline?.Name ?? "Unknown",
                    onFailure: _ => "Unknown");

                return Result<string>.Ok(
                    $"# Pipeline YAML not available for run {runId}\n# Pipeline: {pipelineName}");
            });
    }

    /// <summary>
    /// Get timeline for a pipeline run to find failed steps (functional approach with Result).
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get
    /// </summary>
    public Task<Result<Timeline>> GetTimelineAsync(string org, string project, int runId) =>
        TryAsync(async () =>
        {
            var url = BuildTimelineUrl(org, project, runId);
            var json = await FetchJsonAsync(url);
            return DeserializeJson<Timeline>(json);
        });

    /// <summary>
    /// Download log content from a URL (functional approach with Result).
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/logs/get
    /// </summary>
    public Task<Result<string>> GetLogContentAsync(string logUrl) =>
        TryAsync(() => FetchTextAsync(logUrl));

    // Pure functions for URL building
    private static string BuildRunUrl(string org, string project, int runId) =>
        $"https://dev.azure.com/{org}/{project}/_apis/pipelines/runs/{runId}?api-version={ApiVersion}";

    private static string BuildYamlUrl(string org, string project, int runId) =>
        $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{runId}/yaml?api-version={ApiVersion}";

    private static string BuildTimelineUrl(string org, string project, int runId) =>
        $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{runId}/timeline?api-version={ApiVersion}";

    // IO operations (side effects)
    private async Task<string> FetchJsonAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> FetchTextAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // Pure function for deserialization
    private static T DeserializeJson<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
}
