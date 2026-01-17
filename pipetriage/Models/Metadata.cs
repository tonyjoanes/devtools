namespace PipeTriage.Models;

public class Metadata
{
    public int RunId { get; set; }
    public string RunName { get; set; } = string.Empty;
    public string PipelineName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime? Created { get; set; }
    public DateTime? Finished { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}
