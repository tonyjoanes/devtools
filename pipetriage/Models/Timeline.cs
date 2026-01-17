namespace PipeTriage.Models;

public class Timeline
{
    public List<TimelineRecord> Records { get; set; } = new();
}

public class TimelineRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public TimelineLog? Log { get; set; }
}

public class TimelineLog
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
}
