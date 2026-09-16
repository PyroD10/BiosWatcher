namespace BiosWatcher.Models;

public record BiosRelease(
    string Version,
    DateOnly ReleaseDate,
    string Changelog,
    string? DownloadUrl,
    long? SizeBytes,
    string? Sha256,
    bool IsBeta);
