using System.CommandLine;
using PipeTriage;

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

// Create the summarize command
var summarizeCommand = new Command("summarize", "Analyze and summarize a failed pipeline run")
{
    orgOption,
    projectOption,
    runIdOption
};

summarizeCommand.SetHandler(async (org, project, runId) =>
{
    try
    {
        // Get PAT from environment
        var pat = Environment.GetEnvironmentVariable("AZDO_PAT");
        if (string.IsNullOrWhiteSpace(pat))
        {
            Console.Error.WriteLine("Error: AZDO_PAT environment variable is not set.");
            Console.Error.WriteLine("Please set your Azure DevOps Personal Access Token:");
            Console.Error.WriteLine("  export AZDO_PAT=your-token-here");
            Environment.Exit(1);
            return;
        }

        // Create client and service
        var client = new AzureDevOpsClient(pat);
        var service = new TriageService(client);

        // Execute the summarization
        await service.SummarizeRunAsync(org, project, runId);
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"Error communicating with Azure DevOps: {ex.Message}");
        Console.Error.WriteLine("Please check your credentials, organization, project, and run ID.");
        Environment.Exit(1);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, orgOption, projectOption, runIdOption);

// Create root command
var rootCommand = new RootCommand("pipetriage - Azure DevOps pipeline failure triage tool")
{
    summarizeCommand
};

// Execute
return await rootCommand.InvokeAsync(args);
