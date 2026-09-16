using BiosWatcher.Models;

namespace BiosWatcher.Services;

/// <summary>Drives per-board and bulk BIOS version checks; sequential with a polite delay between vendor requests.</summary>
public class UpdateChecker(VendorRegistry registry)
{
    private static readonly TimeSpan DelayBetweenBoards = TimeSpan.FromMilliseconds(500);

    /// <summary>Returns true when the check surfaced a release version not yet in <see cref="Board.NotifiedVersions"/>.</summary>
    public async Task<bool> CheckBoardAsync(Board board, AppSettings settings, CancellationToken cancellationToken = default)
    {
        var source = registry.FindByVendorId(board.VendorId);
        if (source is null)
        {
            board.LastCheckFailed = true;
            board.LastCheckError = $"No source registered for vendor '{board.VendorId}'.";
            board.LastChecked = DateTimeOffset.UtcNow;
            return false;
        }

        var isNewVersion = false;
        try
        {
            var boardRef = new BoardRef(board.VendorId, board.ModelId, board.SupportUrl);
            var info = await source.GetBoardInfoAsync(boardRef, cancellationToken);
            var latest = SelectLatestRelease(info.Releases, settings.IgnoreBetaVersions);

            if (latest is not null)
            {
                if (!board.NotifiedVersions.Contains(latest.Version))
                {
                    isNewVersion = true;
                    board.NotifiedVersions.Add(latest.Version);
                }

                board.LastKnownVersion = latest.Version;
                board.LastKnownReleaseDate = latest.ReleaseDate;
            }

            board.LastCheckFailed = false;
            board.LastCheckError = null;
        }
        catch (Exception ex)
        {
            // Keep last known data on any fetch/parse failure; one broken vendor must not block the others.
            board.LastCheckFailed = true;
            board.LastCheckError = ex.Message;
        }
        finally
        {
            board.LastChecked = DateTimeOffset.UtcNow;
        }

        return isNewVersion;
    }

    public async Task CheckAllAsync(
        IEnumerable<Board> boards,
        AppSettings settings,
        Action<Board, bool>? onBoardChecked = null,
        CancellationToken cancellationToken = default)
    {
        var isFirst = true;
        foreach (var board in boards)
        {
            if (!isFirst)
                await Task.Delay(DelayBetweenBoards, cancellationToken);
            isFirst = false;

            var isNewVersion = await CheckBoardAsync(board, settings, cancellationToken);
            onBoardChecked?.Invoke(board, isNewVersion);
        }
    }

    internal static BiosRelease? SelectLatestRelease(IReadOnlyList<BiosRelease> releases, bool ignoreBeta)
    {
        var candidates = ignoreBeta ? releases.Where(r => !r.IsBeta) : releases;
        return candidates.OrderByDescending(r => r.ReleaseDate).FirstOrDefault();
    }
}
