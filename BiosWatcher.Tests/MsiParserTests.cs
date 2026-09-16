using System.Text.Json;
using BiosWatcher.Services.Sources;

namespace BiosWatcher.Tests;

public class MsiParserTests
{
    private static MsiSupportResponse LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MsiSupportResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void ParsesTitleAndAllReleasesFromFixture()
    {
        var payload = LoadFixture("msi_mag_b650_tomahawk_wifi.json");

        Assert.Equal(200, payload.Status!.Code);
        Assert.Equal("MAG B650 TOMAHAWK WIFI", payload.Result!.Title);
        Assert.Equal(3, payload.Result.Downloads!.AmiBios!.Count);
    }

    [Fact]
    public void StripsBetaSuffixFromVersionAndFlagsIsBeta()
    {
        var payload = LoadFixture("msi_mag_b650_tomahawk_wifi.json");
        var betaEntry = payload.Result!.Downloads!.AmiBios!.Single(e => e.Version!.Contains("Beta"));

        var release = MsiBiosSource.ParseRelease(betaEntry);

        Assert.Equal("7D75v1R1", release.Version);
        Assert.True(release.IsBeta);
    }

    [Fact]
    public void NonBetaEntryIsNotFlaggedAsBeta()
    {
        var payload = LoadFixture("msi_mag_b650_tomahawk_wifi.json");
        var entry = payload.Result!.Downloads!.AmiBios!.Single(e => e.Version == "7D75v1R2");

        var release = MsiBiosSource.ParseRelease(entry);

        Assert.False(release.IsBeta);
        Assert.Equal(new DateOnly(2026, 7, 6), release.ReleaseDate);
    }

    [Fact]
    public void StripsShaPrefixAndTrailingBrFromChecksum()
    {
        var payload = LoadFixture("msi_mag_b650_tomahawk_wifi.json");
        var entry = payload.Result!.Downloads!.AmiBios!.Single(e => e.Version == "7D75v1R2");

        var release = MsiBiosSource.ParseRelease(entry);

        Assert.NotNull(release.Sha256);
        Assert.DoesNotContain("SHA-256:", release.Sha256);
        Assert.DoesNotContain("<br>", release.Sha256);
    }

    [Fact]
    public void MissingChecksumBecomesNull()
    {
        var payload = LoadFixture("msi_mag_b650_tomahawk_wifi.json");
        var entry = payload.Result!.Downloads!.AmiBios!.Single(e => e.Version == "7D75v14");

        var release = MsiBiosSource.ParseRelease(entry);

        Assert.Null(release.Sha256);
    }

    [Theory]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support#bios", true)]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", true)]
    [InlineData("https://www.asus.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", false)]
    [InlineData("https://www.msi.com/Laptop/Foo/support", false)]
    public void CanHandleDetectsMsiMotherboardSupportUrls(string url, bool expected)
    {
        var source = new MsiBiosSource();

        Assert.Equal(expected, source.CanHandle(new Uri(url)));
    }

    [Fact]
    public void ParseBoardRefExtractsSlugFromSupportUrl()
    {
        var source = new MsiBiosSource();
        var uri = new Uri("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support#bios");

        var boardRef = source.ParseBoardRef(uri);

        Assert.Equal("msi", boardRef.VendorId);
        Assert.Equal("MAG-B650-TOMAHAWK-WIFI", boardRef.ModelId);
        Assert.Equal(uri.ToString(), boardRef.SupportUrl);
    }

    [Fact]
    public void ParseBoardRefThrowsForNonMsiUrl()
    {
        var source = new MsiBiosSource();

        Assert.Throws<ArgumentException>(() => source.ParseBoardRef(new Uri("https://www.msi.com/Laptop/Foo/support")));
    }
}
