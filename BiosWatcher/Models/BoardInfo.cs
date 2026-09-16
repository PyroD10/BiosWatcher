namespace BiosWatcher.Models;

public record BoardInfo(string Title, IReadOnlyList<BiosRelease> Releases);
