using BiosWatcher.Models;
using BiosWatcher.Services;
using BiosWatcher.Services.Sources;

namespace BiosWatcher.Tests;

public class UpdateCheckerTests
{
    private static BiosRelease Release(string version, DateOnly date, bool isBeta = false) =>
        new(version, date, "changelog", null, null, null, isBeta);

    private sealed class FakeBiosSource(string vendorId, Func<BoardRef, Task<BoardInfo>> handler) : IBiosSource
    {
        public string VendorId { get; } = vendorId;

        public bool CanHandle(Uri supportUrl) => true;

        public BoardRef ParseBoardRef(Uri supportUrl) => new(VendorId, "model", supportUrl.ToString());

        public Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default) =>
            handler(boardRef);
    }

    private static Board NewBoard(string vendorId) => new()
    {
        Id = "1",
        VendorId = vendorId,
        ModelId = "model",
        DisplayName = "Test Board",
        SupportUrl = "https://example.com/board",
    };

    [Fact]
    public void SelectLatestRelease_ReturnsNull_WhenNoReleases()
    {
        var result = UpdateChecker.SelectLatestRelease([], ignoreBeta: true);

        Assert.Null(result);
    }

    [Fact]
    public void SelectLatestRelease_PicksNewestByDate()
    {
        var releases = new List<BiosRelease>
        {
            Release("1.0", new DateOnly(2026, 1, 1)),
            Release("2.0", new DateOnly(2026, 6, 1)),
            Release("1.5", new DateOnly(2026, 3, 1)),
        };

        var result = UpdateChecker.SelectLatestRelease(releases, ignoreBeta: true);

        Assert.Equal("2.0", result!.Version);
    }

    [Fact]
    public void SelectLatestRelease_SkipsBetaWhenIgnoreBetaIsTrue()
    {
        var releases = new List<BiosRelease>
        {
            Release("1.0", new DateOnly(2026, 1, 1)),
            Release("2.0-beta", new DateOnly(2026, 6, 1), isBeta: true),
        };

        var result = UpdateChecker.SelectLatestRelease(releases, ignoreBeta: true);

        Assert.Equal("1.0", result!.Version);
    }

    [Fact]
    public void SelectLatestRelease_IncludesBetaWhenIgnoreBetaIsFalse()
    {
        var releases = new List<BiosRelease>
        {
            Release("1.0", new DateOnly(2026, 1, 1)),
            Release("2.0-beta", new DateOnly(2026, 6, 1), isBeta: true),
        };

        var result = UpdateChecker.SelectLatestRelease(releases, ignoreBeta: false);

        Assert.Equal("2.0-beta", result!.Version);
    }

    [Fact]
    public void SelectLatestRelease_ReturnsNull_WhenOnlyBetaExistsAndIgnored()
    {
        var releases = new List<BiosRelease> { Release("2.0-beta", new DateOnly(2026, 6, 1), isBeta: true) };

        var result = UpdateChecker.SelectLatestRelease(releases, ignoreBeta: true);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckBoardAsync_UpdatesLastKnownVersion_OnSuccess()
    {
        var source = new FakeBiosSource("fake", _ =>
            Task.FromResult(new BoardInfo("Title", [Release("9.9", new DateOnly(2026, 8, 1))])));
        var checker = new UpdateChecker(new VendorRegistry([source]));
        var board = NewBoard("fake");

        await checker.CheckBoardAsync(board, new AppSettings());

        Assert.Equal("9.9", board.LastKnownVersion);
        Assert.Equal(new DateOnly(2026, 8, 1), board.LastKnownReleaseDate);
        Assert.False(board.LastCheckFailed);
        Assert.NotNull(board.LastChecked);
    }

    [Fact]
    public async Task CheckBoardAsync_KeepsLastKnownData_OnFetchFailure()
    {
        var source = new FakeBiosSource("fake", _ => throw new HttpRequestException("boom"));
        var checker = new UpdateChecker(new VendorRegistry([source]));
        var board = NewBoard("fake");
        board.LastKnownVersion = "1.0";
        board.LastKnownReleaseDate = new DateOnly(2026, 1, 1);

        await checker.CheckBoardAsync(board, new AppSettings());

        Assert.Equal("1.0", board.LastKnownVersion);
        Assert.Equal(new DateOnly(2026, 1, 1), board.LastKnownReleaseDate);
        Assert.True(board.LastCheckFailed);
    }

    [Fact]
    public async Task CheckBoardAsync_FailsGracefully_WhenVendorNotRegistered()
    {
        var checker = new UpdateChecker(new VendorRegistry([]));
        var board = NewBoard("unknown-vendor");

        await checker.CheckBoardAsync(board, new AppSettings());

        Assert.True(board.LastCheckFailed);
        Assert.NotNull(board.LastChecked);
    }

    [Fact]
    public async Task CheckAllAsync_OneBoardFailing_DoesNotBlockOthers()
    {
        var failingSource = new FakeBiosSource("fail", _ => throw new HttpRequestException("boom"));
        var okSource = new FakeBiosSource("ok", _ =>
            Task.FromResult(new BoardInfo("Title", [Release("3.0", new DateOnly(2026, 5, 5))])));
        var checker = new UpdateChecker(new VendorRegistry([failingSource, okSource]));

        var failingBoard = NewBoard("fail");
        var okBoard = NewBoard("ok");

        await checker.CheckAllAsync([failingBoard, okBoard], new AppSettings());

        Assert.True(failingBoard.LastCheckFailed);
        Assert.False(okBoard.LastCheckFailed);
        Assert.Equal("3.0", okBoard.LastKnownVersion);
    }

    [Fact]
    public async Task CheckBoardAsync_ReturnsTrue_WhenReleaseVersionNotYetNotified()
    {
        var source = new FakeBiosSource("fake", _ =>
            Task.FromResult(new BoardInfo("Title", [Release("9.9", new DateOnly(2026, 8, 1))])));
        var checker = new UpdateChecker(new VendorRegistry([source]));
        var board = NewBoard("fake");
        board.LastKnownVersion = "9.0";
        board.NotifiedVersions = ["9.0"];

        var isNewVersion = await checker.CheckBoardAsync(board, new AppSettings());

        Assert.True(isNewVersion);
        Assert.Equal(["9.0", "9.9"], board.NotifiedVersions);
    }

    [Fact]
    public async Task CheckBoardAsync_ReturnsFalse_WhenReleaseAlreadyNotified()
    {
        var source = new FakeBiosSource("fake", _ =>
            Task.FromResult(new BoardInfo("Title", [Release("9.9", new DateOnly(2026, 8, 1))])));
        var checker = new UpdateChecker(new VendorRegistry([source]));
        var board = NewBoard("fake");
        board.LastKnownVersion = "9.9";
        board.NotifiedVersions = ["9.9"];

        var isNewVersion = await checker.CheckBoardAsync(board, new AppSettings());

        Assert.False(isNewVersion);
        Assert.Equal(["9.9"], board.NotifiedVersions);
    }

    [Fact]
    public async Task CheckBoardAsync_ReturnsFalse_OnFetchFailure()
    {
        var source = new FakeBiosSource("fake", _ => throw new HttpRequestException("boom"));
        var checker = new UpdateChecker(new VendorRegistry([source]));
        var board = NewBoard("fake");

        var isNewVersion = await checker.CheckBoardAsync(board, new AppSettings());

        Assert.False(isNewVersion);
    }
}
