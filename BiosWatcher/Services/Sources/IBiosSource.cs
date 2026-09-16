using BiosWatcher.Models;

namespace BiosWatcher.Services.Sources;

public interface IBiosSource
{
    string VendorId { get; }

    bool CanHandle(Uri supportUrl);

    BoardRef ParseBoardRef(Uri supportUrl);

    Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default);
}
