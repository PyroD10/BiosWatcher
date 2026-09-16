using System.Text.Json.Serialization;

namespace BiosWatcher.Models;

public class Board
{
    public required string Id { get; set; }
    public required string VendorId { get; set; }
    public required string ModelId { get; set; }
    public required string DisplayName { get; set; }
    public required string SupportUrl { get; set; }
    public string? LastKnownVersion { get; set; }
    public DateOnly? LastKnownReleaseDate { get; set; }
    public DateTimeOffset? LastChecked { get; set; }
    public bool LastCheckFailed { get; set; }
    public List<string> NotifiedVersions { get; set; } = [];

    /// <summary>Detail for the row's error indicator; in-memory only, re-populated on the next check, not worth persisting.</summary>
    [JsonIgnore]
    public string? LastCheckError { get; set; }

    /// <summary>NEW is derived from the release date, never stored, so it cannot go stale.</summary>
    public bool IsNew(int newFlagDays) =>
        LastKnownReleaseDate is { } releaseDate &&
        DateOnly.FromDateTime(DateTime.Today).DayNumber - releaseDate.DayNumber <= newFlagDays;
}
