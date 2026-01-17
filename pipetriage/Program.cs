using System.CommandLine;
using PipeTriage;
using PipeTriage.Core;
using PipeTriage.Models;

// Define CLI options
var orgOption = new Option<string>(
    name: "--org",
    description: "Azure DevOps organization name")
{
    IsRequired = true
};

var projectOption = new Option<string>(
    name: "--project",
    description: "Azure DevOps project name")
{
    IsRequired = true
};

var runIdOption = new Option<int>(
    name: "--run-id",
    description: "Pipeline run ID to analyze")
{
    IsRequired = true
};

var outputDirOption = new Option<string>(
    name: "--output",
    description: "Output directory for triage files",
    getDefaultValue: () => ".pipetriage");

// Create the summarize command
var summarizeCommand = new Command("summarize", "Analyze and summarize a failed pipeline run")
{
    orgOption,
    projectOption,
    runIdOption,
    outputDirOption
};

summarizeCommand.SetHandler(async (org, project, runId, outputDir) =>
{
    var exitCode = await ExecuteTriageAsync(org, project, runId, outputDir);
    Environment.Exit(exitCode);
}, orgOption, projectOption, runIdOption, outputDirOption);

// Create root command
var rootCommand = new RootCommand("pipetriage - Azure DevOps pipeline failure triage tool")
{
    summarizeCommand
};

// Execute
return await rootCommand.InvokeAsync(args);

// Pure functions and functional composition

/// <summary>
/// Main execution function using functional error handling.
/// </summary>
static async Task<int> ExecuteTriageAsync(string org, string project, int runId, string outputDir)
{
    var result = await GetPatFromEnvironment()
        .Bind(pat => CreateTriageService(pat))
        .BindAsync(async service => await RunTriageAnalysisAsync(service, org, project, runId, outputDir));

    return result.Match(
        onSuccess: _ => 0,
        onFailure: error =>
        {
            Console.Error.WriteLine(error);
            return 1;
        });
}

/// <summary>
/// Pure function to retrieve PAT from environment.
/// </summary>
static Result<string> GetPatFromEnvironment()
{
    var pat = Environment.GetEnvironmentVariable("AZDO_PAT");

    return string.IsNullOrWhiteSpace(pat)
        ? Result<string>.Fail(
            "Error: AZDO_PAT environment variable is not set.\n" +
            "Please set your Azure DevOps Personal Access Token:\n" +
            "  export AZDO_PAT=your-token-here")
        : Result<string>.Ok(pat);
}

/// <summary>
/// Pure function to create triage service with dependencies.
/// </summary>
static Result<TriageService> CreateTriageService(string pat)
{
    try
    {
        var httpClient = new HttpClient();
        var client = new AzureDevOpsClient(httpClient, pat);
        var service = new TriageService(client);
        return Result<TriageService>.Ok(service);
    }
    catch (Exception ex)
    {
        return Result<TriageService>.Fail($"Failed to create triage service: {ex.Message}");
    }
}

/// <summary>
/// Executes the triage analysis pipeline.
/// </summary>
static async Task<Result<TriageOutput>> RunTriageAnalysisAsync(
    TriageService service,
    string org,
    string project,
    int runId,
    string outputDir)
{
    var request = new TriageRequest(org, project, runId, outputDir);

    try
    {
        return await service.AnalyzeRunAsync(request);
    }
    catch (Exception ex)
    {
        return Result<TriageOutput>.Fail(
            $"Error during triage analysis: {ex.Message}\n" +
            "Please check your credentials, organization, project, and run ID.");
    }
}
