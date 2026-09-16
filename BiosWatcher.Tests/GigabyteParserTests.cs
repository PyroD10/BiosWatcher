using BiosWatcher.Services.Sources;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Tests;

public class GigabyteParserTests
{
    private static HtmlDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var doc = new HtmlDocument();
        doc.Load(path);
        return doc;
    }

    [Fact]
    public void ParsesTitleFromPageHeading()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var title = GigabyteBiosSource.ParseTitle(doc, fallback: "fallback");

        Assert.Equal("X670E AORUS MASTER (rev. 1.x)", title);
    }

    [Fact]
    public void ParsesAllBiosRowsButNotTheHeaderRow()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);

        Assert.Equal(4, releases.Count);
        Assert.DoesNotContain(releases, r => r.Version == "Version");
    }

    [Fact]
    public void SortsReleasesNewestFirst()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);

        Assert.Equal("F21a", releases[0].Version);
        Assert.Equal(new DateOnly(2023, 12, 21), releases[0].ReleaseDate);
    }

    [Fact]
    public void ParsesZeroPaddedDayDateFormat()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);
        var f11 = releases.Single(r => r.Version == "F11");

        Assert.Equal(new DateOnly(2023, 6, 7), f11.ReleaseDate);
    }

    [Fact]
    public void ExtractsChecksumFromFirstListItemIntoSha256Slot()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);
        var f21a = releases.Single(r => r.Version == "F21a");

        Assert.Equal("934C", f21a.Sha256);
        Assert.DoesNotContain("Checksum", f21a.Changelog);
    }

    [Fact]
    public void NeverFlagsAnyReleaseAsBeta()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);

        Assert.All(releases, r => Assert.False(r.IsBeta));
    }

    [Fact]
    public void ParsesDownloadSizeStringToBytes()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);
        var f21a = releases.Single(r => r.Version == "F21a");

        Assert.Equal((long)Math.Round(10.34 * 1024 * 1024), f21a.SizeBytes);
    }

    [Fact]
    public void ParsesDownloadUrlFromAnchorHref()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        var releases = GigabyteBiosSource.ParseReleases(doc);
        var f21a = releases.Single(r => r.Version == "F21a");

        Assert.Equal(
            "https://download.gigabyte.com/FileList/BIOS/mb_bios_x670e-aorus-master_8arpl002_f21a.zip?v=b8eb8c09b29078a7761c3310f5db1490",
            f21a.DownloadUrl);
    }

    [Fact]
    public void CapturesFootnoteTextThatSitsOutsideTheListInTheDescDiv()
    {
        var doc = LoadFixture("gigabyte_x670e_aorus_master.html");

        // F13's footnote is in a trailing <p>; F13d's is a bare text node after </ol>. Both must survive
        // into the changelog rather than being silently dropped by an <li>-only extraction.
        var releases = GigabyteBiosSource.ParseReleases(doc);
        var f13 = releases.Single(r => r.Version == "F13");
        var f13d = releases.Single(r => r.Version == "F13d");

        Assert.Contains("Higher memory speeds may cause longer memory training time", f13.Changelog);
        Assert.Contains("Higher memory speeds may cause longer memory training time", f13d.Changelog);
    }

    [Theory]
    [InlineData("https://www.gigabyte.com/Motherboard/X670E-AORUS-MASTER-rev-1x/support#support-dl-bios", true)]
    [InlineData("https://www.gigabyte.com/us/Motherboard/X670E-AORUS-MASTER-rev-1x/support", true)]
    [InlineData("https://www.gigabyte.com/Laptop/AORUS-15/support", false)]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", false)]
    public void CanHandleDetectsGigabyteMotherboardSupportUrls(string url, bool expected)
    {
        var source = new GigabyteBiosSource();

        Assert.Equal(expected, source.CanHandle(new Uri(url)));
    }

    [Fact]
    public void ParseBoardRefKeepsRevisionSuffixAsPartOfModelId()
    {
        var source = new GigabyteBiosSource();
        var uri = new Uri("https://www.gigabyte.com/Motherboard/X670E-AORUS-MASTER-rev-1x/support#support-dl-bios");

        var boardRef = source.ParseBoardRef(uri);

        Assert.Equal("gigabyte", boardRef.VendorId);
        Assert.Equal("X670E-AORUS-MASTER-rev-1x", boardRef.ModelId);
    }
}
