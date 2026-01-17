using System.Text.Json;
using PipeTriage.Models;

namespace PipeTriage;

public class TriageService
{
    private readonly AzureDevOpsClient _client;

    public TriageService(AzureDevOpsClient client)
    {
        _client = client;
    }

    public async Task SummarizeRunAsync(string org, string project, int runId, string outputRootDir = ".pipetriage")
    {
        Console.WriteLine($"Fetching run details for run {runId}...");

        // 1. Get run metadata
        var run = await _client.GetRunAsync(org, project, runId);

        // Extract repository and branch information
        var repository = run.Resources?.Repositories?.Self?.Repository?.Name ?? "Unknown";
        var branch = run.Resources?.Repositories?.Self?.RefName ?? "Unknown";

        // Create metadata object
        var metadata = new Metadata
        {
            RunId = run.Id,
            RunName = run.Name,
            PipelineName = run.Pipeline?.Name ?? "Unknown",
            State = run.State,
            Result = run.Result,
            Created = run.CreatedDate,
            Finished = run.FinishedDate,
            Repository = repository,
            Branch = branch
        };

        // 2. Create output directory
        var outputDir = Path.Combine(outputRootDir, runId.ToString());
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "logs"));

        // 3. Write metadata.json
        var metadataPath = Path.Combine(outputDir, "metadata.json");
        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(metadataPath, metadataJson);
        Console.WriteLine($"✓ Wrote metadata to {metadataPath}");

        // 4. Get and write pipeline YAML
        Console.WriteLine("Fetching pipeline YAML...");
        var yaml = await _client.GetPipelineYamlAsync(org, project, runId);
        var yamlPath = Path.Combine(outputDir, "pipeline.yaml");
        await File.WriteAllTextAsync(yamlPath, yaml);
        Console.WriteLine($"✓ Wrote pipeline YAML to {yamlPath}");

        // 5. Get timeline and find failed step
        Console.WriteLine("Analyzing timeline for failed steps...");
        var timeline = await _client.GetTimelineAsync(org, project, runId);

        var failedRecord = timeline.Records.FirstOrDefault(r =>
            r.Result?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true);

        if (failedRecord != null && failedRecord.Log != null)
        {
            Console.WriteLine($"Found failed step: {failedRecord.Name}");
            Console.WriteLine("Downloading log...");

            var logContent = await _client.GetLogContentAsync(failedRecord.Log.Url);
            var logPath = Path.Combine(outputDir, "logs", "failed-step.log");
            await File.WriteAllTextAsync(logPath, logContent);
            Console.WriteLine($"✓ Wrote failed step log to {logPath}");

            // Print summary
            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Pipeline: {metadata.PipelineName} (run {metadata.RunId}) – Result: {metadata.Result}");
            Console.WriteLine($"Repo: {metadata.Repository}, Branch: {metadata.Branch}");
            Console.WriteLine($"Failed step: {failedRecord.Name} – log: logs/failed-step.log");
            Console.WriteLine($"All files written to: {outputDir}");
        }
        else
        {
            Console.WriteLine("⚠ No failed steps found in timeline");
            Console.WriteLine();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Pipeline: {metadata.PipelineName} (run {metadata.RunId}) – Result: {metadata.Result}");
            Console.WriteLine($"Repo: {metadata.Repository}, Branch: {metadata.Branch}");
            Console.WriteLine($"All files written to: {outputDir}");
        }
    }
}
