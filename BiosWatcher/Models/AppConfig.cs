namespace BiosWatcher.Models;

public class AppSettings
{
    public int CheckIntervalHours { get; set; } = 24;
    public bool IgnoreBetaVersions { get; set; } = true;
    public int NewFlagDays { get; set; } = 14;
}

public class AppConfig
{
    public AppSettings Settings { get; set; } = new();
    public List<Board> Boards { get; set; } = [];
}
