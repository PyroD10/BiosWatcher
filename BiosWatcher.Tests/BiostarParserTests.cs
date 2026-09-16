using BiosWatcher.Services.Sources;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Tests;

public class BiostarParserTests
{
    private static HtmlDocument LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var doc = new HtmlDocument();
        doc.Load(path);
        return doc;
    }

    [Theory]
    [InlineData("https://www.biostar.com.tw/app/en/mb/introduction.php?S_ID=1079", true)]
    [InlineData("https://www.biostar.com.tw/app/tw/mb/introduction.php?S_ID=1157", true)]
    [InlineData("https://www.biostar.com.tw/app/en/mb/introduction.php", false)]
    [InlineData("https://www.biostar.com.tw/app/en/support/download.php?S_ID=1079", false)]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", false)]
    public void CanHandleDetectsBiostarProductUrls(string url, bool expected)
    {
        var source = new BiostarBiosSource();

        Assert.Equal(expected, source.CanHandle(new Uri(url)));
    }

    [Fact]
    public void ParseBoardRefExtractsNumericSId()
    {
        var source = new BiostarBiosSource();
        var uri = new Uri("https://www.biostar.com.tw/app/en/mb/introduction.php?S_ID=1079");

        var boardRef = source.ParseBoardRef(uri);

        Assert.Equal("biostar", boardRef.VendorId);
        Assert.Equal("1079", boardRef.ModelId);
    }

    [Fact]
    public void ParseBoardRefThrowsForNonBiostarProductUrl()
    {
        var source = new BiostarBiosSource();

        Assert.Throws<ArgumentException>(() => source.ParseBoardRef(new Uri("https://www.biostar.com.tw/app/en/mb/introduction.php")));
    }

    [Fact]
    public void ParseTitleReadsBreadcrumbMainHeading()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_intro.html");

        var title = BiostarBiosSource.ParseTitle(doc, fallback: "1079");

        Assert.Equal("X670E VALKYRIE", title);
    }

    [Fact]
    public void ParseTitleFallsBackWhenHeadingMissing()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>no title here</div>");

        var title = BiostarBiosSource.ParseTitle(doc, fallback: "1079");

        Assert.Equal("1079", title);
    }

    [Fact]
    public void ParsesAllRealBiosRowsExcludingTrailingGuidePdfRows()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);

        Assert.Equal(19, releases.Count);
    }

    [Fact]
    public void FiltersOutTrailingGuidePdfRowsThatLackAVersionCell()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);

        Assert.DoesNotContain(releases, r => r.Version == "bios_update.pdf" || r.Version == "Smart_bios_update_X670E.pdf");
    }

    [Fact]
    public void SortsReleasesNewestFirst()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);

        Assert.Equal("X67AE416.BST", releases[0].Version);
        Assert.Equal(new DateOnly(2026, 4, 16), releases[0].ReleaseDate);
    }

    [Fact]
    public void ParsesIsoDateFormat()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);
        var oldest = releases.Single(r => r.Version == "X67AE831.BST");

        Assert.Equal(new DateOnly(2022, 8, 31), oldest.ReleaseDate);
    }

    [Fact]
    public void ParsesKilobyteSizeString()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "X67AE416.BST");

        Assert.Equal(32768L * 1024, release.SizeBytes);
    }

    [Fact]
    public void EveryReleaseIsNotBeta()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);

        Assert.All(releases, r => Assert.False(r.IsBeta));
    }

    [Fact]
    public void Sha256IsAlwaysNullSinceVendorProvidesNoChecksum()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);

        Assert.All(releases, r => Assert.Null(r.Sha256));
    }

    [Fact]
    public void ConvertsBrTagInDescriptionToNewline()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "X67AE426.BST");

        Assert.Contains("Update AGESA ComboAM5 PI 1.1.7.0 Patch A", release.Changelog);
        Assert.Contains("Support AMD next generation CPU", release.Changelog);
        Assert.DoesNotContain("<br>", release.Changelog);
    }

    [Fact]
    public void ExtractsRealDownloadUrlFromOnclickLightboxCall()
    {
        var doc = LoadFixture("biostar_x670e_valkyrie_download.html");

        var releases = BiostarBiosSource.ParseReleases(doc);
        var release = releases.Single(r => r.Version == "X67AE416.BST");

        Assert.Equal("https://www.biostar.com.tw/upload/Bios/X67AE416BST.zip", release.DownloadUrl);
    }
}
