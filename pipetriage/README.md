# pipetriage

A .NET 8 CLI tool for triaging failed Azure DevOps pipeline runs.

## Features

- Fetches run metadata, pipeline YAML, and logs from Azure DevOps
- Identifies the first failed step in a pipeline run
- Organizes all relevant information in a structured directory
- Provides a console summary of the failure
- Built with functional programming best practices for robustness and maintainability

## Architecture

This tool follows functional programming principles in C#:

- **Immutability**: All domain models use `record` types for immutable data structures
- **Result Types**: Error handling via `Result<T>` monad instead of exceptions
- **Pure Functions**: Business logic separated from IO operations
- **Interfaces**: Dependency injection with `IAzureDevOpsClient` for testability
- **Expression-Bodied Members**: Concise, readable function definitions
- **Pattern Matching**: Type-safe branching with `switch` expressions

## Prerequisites

- .NET 8 SDK
- Azure DevOps Personal Access Token (PAT) with read access to:
  - Build
  - Code (for pipeline YAML)

## Installation

```bash
dotnet build
dotnet publish -c Release
```

## Usage

1. Set your Azure DevOps PAT as an environment variable:

```bash
export AZDO_PAT=your-personal-access-token
```

2. Run the tool:

```bash
dotnet run -- summarize --org <organization> --project <project> --run-id <run-id>
```

Or if using the published version:

```bash
./pipetriage summarize --org myorg --project MyProject --run-id 12345
```

Optional: Specify a custom output directory:

```bash
./pipetriage summarize --org myorg --project MyProject --run-id 12345 --output ./my-triage-reports
```

## Output

The tool creates a `.pipetriage/<run-id>/` directory in your current working directory containing:

- `metadata.json` - Run metadata including pipeline name, state, result, dates, repo, and branch
- `pipeline.yaml` - The pipeline definition (if available)
- `logs/failed-step.log` - Log output from the first failed step

## Example

```bash
$ export AZDO_PAT=abc123...
$ dotnet run -- summarize --org contoso --project MyApp --run-id 42

Fetching run details for run 42...
✓ Wrote metadata to .pipetriage/42/metadata.json
Fetching pipeline YAML...
✓ Wrote pipeline YAML to .pipetriage/42/pipeline.yaml
Analyzing timeline for failed steps...
Found failed step: Run tests
Downloading log...
✓ Wrote failed step log to .pipetriage/42/logs/failed-step.log

=== Summary ===
Pipeline: CI Build (run 42) – Result: failed
Repo: MyApp, Branch: refs/heads/main
Failed step: Run tests – log: logs/failed-step.log
All files written to: .pipetriage/42
```

## API References

This tool uses the following Azure DevOps REST APIs:

- [Runs - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/get)
- [Yaml - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/build/yaml/get)
- [Timeline - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get)
- [Logs - Get](https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/logs/get)

## License

MIT
