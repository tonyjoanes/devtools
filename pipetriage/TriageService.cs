using System.Text.Json;
using PipeTriage.Core;
using PipeTriage.Models;

namespace PipeTriage;

/// <summary>
/// Functional triage service that orchestrates pipeline analysis.
/// Separates pure domain logic from IO operations.
/// </summary>
public sealed class TriageService
{
    private readonly IAzureDevOpsClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public TriageService(IAzureDevOpsClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Main entry point for pipeline triage using functional composition.
    /// </summary>
    public async Task<Result<TriageOutput>> AnalyzeRunAsync(TriageRequest request)
    {
        Console.WriteLine($"Fetching run details for run {request.RunId}...");

        // Compose the pipeline using Result monad
        var result = await FetchTriageDataAsync(request)
            .BindAsync(async data => await ProcessTriageDataAsync(data, request));

        return result;
    }

    /// <summary>
    /// Fetches all required data from Azure DevOps APIs (IO operation).
    /// </summary>
    private async Task<Result<(RunDetails run, string yaml, Timeline timeline)>> FetchTriageDataAsync(
        TriageRequest request)
    {
        var runResult = await _client.GetRunAsync(
            request.Organization,
            request.Project,
            request.RunId);

        if (runResult.IsFailure)
            return Result<(RunDetails, string, Timeline)>.Fail(runResult.Match(_ => "", err => err));

        var yamlResult = await _client.GetPipelineYamlAsync(
            request.Organization,
            request.Project,
            request.RunId);

        if (yamlResult.IsFailure)
            return Result<(RunDetails, string, Timeline)>.Fail(yamlResult.Match(_ => "", err => err));

        var timelineResult = await _client.GetTimelineAsync(
            request.Organization,
            request.Project,
            request.RunId);

        if (timelineResult.IsFailure)
            return Result<(RunDetails, string, Timeline)>.Fail(timelineResult.Match(_ => "", err => err));

        return runResult.Bind(run =>
            yamlResult.Bind(yaml =>
                timelineResult.Map(timeline => (run, yaml, timeline))));
    }

    /// <summary>
    /// Processes fetched data into triage output (pure + IO).
    /// </summary>
    private async Task<Result<TriageOutput>> ProcessTriageDataAsync(
        (RunDetails run, string yaml, Timeline timeline) data,
        TriageRequest request)
    {
        // Pure: Extract metadata
        var metadata = Metadata.FromRunDetails(data.run);

        // Pure: Find failed step
        var failedRecord = FindFirstFailedStep(data.timeline);

        // IO: Fetch log if failed step exists
        var failedStepInfo = failedRecord is not null
            ? await FetchFailedStepInfoAsync(failedRecord)
            : null;

        // Pure: Create output
        var output = new TriageOutput(metadata, data.yaml, failedStepInfo);

        // IO: Write files
        await WriteTriageOutputAsync(output, request);

        // IO: Print summary
        PrintSummary(output, request.OutputDirectory);

        return Result<TriageOutput>.Ok(output);
    }

    /// <summary>
    /// Pure function to find the first failed step in a timeline.
    /// </summary>
    private static TimelineRecord? FindFirstFailedStep(Timeline timeline) =>
        timeline.Records.FirstOrDefault(r => r.IsFailed() && r.Log is not null);

    /// <summary>
    /// Fetches failed step information including logs (IO operation).
    /// </summary>
    private async Task<FailedStepInfo?> FetchFailedStepInfoAsync(TimelineRecord record)
    {
        if (record.Log is null)
            return null;

        Console.WriteLine($"Found failed step: {record.Name}");
        Console.WriteLine("Downloading log...");

        var logResult = await _client.GetLogContentAsync(record.Log.Url);

        return logResult.Match(
            onSuccess: content => new FailedStepInfo(record.Name, content),
            onFailure: _ => null);
    }

    /// <summary>
    /// Writes triage output to disk (IO operation).
    /// </summary>
    private async Task WriteTriageOutputAsync(TriageOutput output, TriageRequest request)
    {
        var outputDir = BuildOutputDirectory(request);
        CreateOutputDirectories(outputDir);

        // Write metadata
        Console.WriteLine("Fetching pipeline YAML...");
        await WriteJsonFileAsync(
            Path.Combine(outputDir, "metadata.json"),
            output.Metadata);
        Console.WriteLine($"✓ Wrote metadata to {Path.Combine(outputDir, "metadata.json")}");

        // Write YAML
        await WriteTextFileAsync(
            Path.Combine(outputDir, "pipeline.yaml"),
            output.PipelineYaml);
        Console.WriteLine($"✓ Wrote pipeline YAML to {Path.Combine(outputDir, "pipeline.yaml")}");

        // Write failed step log if exists
        if (output.FailedStep is not null)
        {
            await WriteTextFileAsync(
                Path.Combine(outputDir, "logs", "failed-step.log"),
                output.FailedStep.LogContent);
            Console.WriteLine($"✓ Wrote failed step log to {Path.Combine(outputDir, "logs", "failed-step.log")}");
        }
    }

    /// <summary>
    /// Prints summary to console (IO operation).
    /// </summary>
    private static void PrintSummary(TriageOutput output, string outputDir)
    {
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Pipeline: {output.Metadata.PipelineName} (run {output.Metadata.RunId}) – Result: {output.Metadata.Result}");
        Console.WriteLine($"Repo: {output.Metadata.Repository}, Branch: {output.Metadata.Branch}");

        if (output.FailedStep is not null)
        {
            Console.WriteLine($"Failed step: {output.FailedStep.StepName} – log: logs/failed-step.log");
        }
        else
        {
            Console.WriteLine("⚠ No failed steps found in timeline");
        }

        Console.WriteLine($"All files written to: {BuildOutputDirectory(new TriageRequest("", "", output.Metadata.RunId, outputDir))}");
    }

    // Pure helper functions
    private static string BuildOutputDirectory(TriageRequest request) =>
        Path.Combine(request.OutputDirectory, request.RunId.ToString());

    // IO helper functions
    private static void CreateOutputDirectories(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "logs"));
    }

    private static async Task WriteJsonFileAsync<T>(string path, T data) =>
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, JsonOptions));

    private static async Task WriteTextFileAsync(string path, string content) =>
        await File.WriteAllTextAsync(path, content);
}
