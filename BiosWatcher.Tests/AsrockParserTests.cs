using BiosWatcher.Services.Sources;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Tests;

public class AsrockParserTests
{
    private static HtmlDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var doc = new HtmlDocument();
        doc.Load(path);
        return doc;
    }

    [Theory]
    [InlineData("https://www.asrock.com/mb/AMD/X670E%20Taichi/index.asp", true)]
    [InlineData("https://www.asrock.com/mb/AMD/B650M-HDV/index.asp", true)]
    [InlineData("https://www.asrock.com/nettop/AMD/4X4%20BOX-5800U/index.asp", false)]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", false)]
    public void CanHandleDetectsAsrockMotherboardProductUrls(string url, bool expected)
    {
        var source = new AsrockBiosSource();

        Assert.Equal(expected, source.CanHandle(new Uri(url)));
    }

    [Fact]
    public void ParseBoardRefDecodesUrlEncodedSpacesInModelId()
    {
        var source = new AsrockBiosSource();
        var uri = new Uri("https://www.asrock.com/mb/AMD/X670E%20Taichi/index.asp");

        var boardRef = source.ParseBoardRef(uri);

        Assert.Equal("asrock", boardRef.VendorId);
        Assert.Equal("X670E Taichi", boardRef.ModelId);
    }

    [Fact]
    public void ParseBoardRefThrowsForNonAsrockMotherboardUrl()
    {
        var source = new AsrockBiosSource();

        Assert.Throws<ArgumentException>(() => source.ParseBoardRef(new Uri("https://www.asrock.com/nettop/AMD/4X4%20BOX-5800U/index.asp")));
    }

    [Fact]
    public void ParsesAllRowsFromFixture()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);

        Assert.Equal(5, releases.Count);
    }

    [Fact]
    public void SortsReleasesNewestFirst()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);

        Assert.Equal("4.43", releases[0].Version);
        Assert.Equal(new DateOnly(2026, 6, 30), releases[0].ReleaseDate);
    }

    [Fact]
    public void ParsesNonZeroPaddedDateFormat()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var oldest = releases.Single(r => r.Version == "1.04");

        Assert.Equal(new DateOnly(2022, 9, 22), oldest.ReleaseDate);
    }

    [Fact]
    public void ParsesSizeStringWithNoSpaceBeforeUnit()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "4.43");

        Assert.Equal((long)Math.Round(19.13 * 1024 * 1024), release.SizeBytes);
    }

    [Fact]
    public void StripsBetaTagFromVersionAndFlagsIsBeta()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var beta = releases.Single(r => r.Version == "3.18.AS02");

        Assert.True(beta.IsBeta);
    }

    [Fact]
    public void NonBetaEntryIsNotFlaggedAsBeta()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "4.43");

        Assert.False(release.IsBeta);
    }

    [Fact]
    public void ExtractsSha256FromEndOfDescriptionCellIntoItsOwnField()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "4.43");

        Assert.Equal("4e22d9dcaab03331be19629de38459a0101b5351a48c8cf61b81ad286c6c3f99", release.Sha256);
        Assert.DoesNotContain("SHA256", release.Changelog);
    }

    [Fact]
    public void KeepsRemarkFootnoteTextInChangelog()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "4.41");

        Assert.Contains("EXPO", release.Changelog);
        Assert.Contains("support may vary by DRAM module", release.Changelog);
    }

    [Fact]
    public void ParsesGlobalHttpsDownloadUrlNotChinaFtpMirror()
    {
        var doc = LoadFixture("asrock_x670e_taichi_bios.html");

        var releases = AsrockBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "4.43");

        Assert.StartsWith("https://download.asrock.com/", release.DownloadUrl);
    }
}
