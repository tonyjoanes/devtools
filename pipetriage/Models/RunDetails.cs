namespace PipeTriage.Models;

public class RunDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Pipeline? Pipeline { get; set; }
    public string State { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime? CreatedDate { get; set; }
    public DateTime? FinishedDate { get; set; }
    public Resources? Resources { get; set; }
}

public class Pipeline
{
    public string Name { get; set; } = string.Empty;
}

public class Resources
{
    public Repository? Repositories { get; set; }
}

public class Repository
{
    public Self? Self { get; set; }
}

public class Self
{
    public RepositoryInfo? Repository { get; set; }
    public string RefName { get; set; } = string.Empty;
}

public class RepositoryInfo
{
    public string Name { get; set; } = string.Empty;
}
