using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PipeTriage.Models;

namespace PipeTriage;

public class AzureDevOpsClient
{
    private readonly HttpClient _httpClient;
    private const string ApiVersion = "7.1";

    public AzureDevOpsClient(string pat)
    {
        _httpClient = new HttpClient();
        var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Get run details using Pipelines Runs API
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/get
    /// </summary>
    public async Task<RunDetails> GetRunAsync(string org, string project, int runId)
    {
        var url = $"https://dev.azure.com/{org}/{project}/_apis/pipelines/runs/{runId}?api-version={ApiVersion}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<RunDetails>(content, options)
            ?? throw new InvalidOperationException("Failed to deserialize run details");
    }

    /// <summary>
    /// Get pipeline YAML using Build YAML API
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/build/yaml/get
    /// </summary>
    public async Task<string> GetPipelineYamlAsync(string org, string project, int runId)
    {
        // First get the run to find the pipeline ID
        var run = await GetRunAsync(org, project, runId);

        // Try to get YAML from the Build API using the run ID as build ID
        var url = $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{runId}/yaml?api-version={ApiVersion}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            // Fallback: return a message if YAML is not available
            return $"# Pipeline YAML not available for run {runId}\n# Pipeline: {run.Pipeline?.Name ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Get timeline for a pipeline run to find failed steps
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get
    /// </summary>
    public async Task<Timeline> GetTimelineAsync(string org, string project, int runId)
    {
        var url = $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{runId}/timeline?api-version={ApiVersion}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<Timeline>(content, options)
            ?? throw new InvalidOperationException("Failed to deserialize timeline");
    }

    /// <summary>
    /// Download log content from a log URL
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/logs/get
    /// </summary>
    public async Task<string> GetLogContentAsync(string logUrl)
    {
        var response = await _httpClient.GetAsync(logUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
