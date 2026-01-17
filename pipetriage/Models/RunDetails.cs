namespace PipeTriage.Models;

/// <summary>
/// Immutable record representing Azure DevOps pipeline run details.
/// </summary>
public sealed record RunDetails(
    int Id,
    string Name,
    Pipeline? Pipeline,
    string State,
    string Result,
    DateTime? CreatedDate,
    DateTime? FinishedDate,
    Resources? Resources
);

public sealed record Pipeline(string Name);

public sealed record Resources(Repository? Repositories);

public sealed record Repository(Self? Self);

public sealed record Self(
    RepositoryInfo? Repository,
    string RefName
);

public sealed record RepositoryInfo(string Name);
