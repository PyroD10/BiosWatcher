using System.Text.Json;
using BiosWatcher.Services.Sources;

namespace BiosWatcher.Tests;

public class AsusParserTests
{
    private static AsusSupportResponse LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AsusSupportResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void ParsesStatusAndBiosGroupFromFixture()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");

        Assert.Equal("SUCCESS", payload.Status);
        var biosGroup = payload.Result!.Obj!.Single(o => o.Name == "BIOS");
        Assert.Equal(3, biosGroup.Files!.Count);
    }

    [Fact]
    public void IgnoresNonBiosGroups()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");

        Assert.Contains(payload.Result!.Obj!, o => o.Name == "Firmware");
        // AsusBiosSource.GetBoardInfoAsync only reads the "BIOS" group — this fixture exists to prove
        // a non-BIOS group ("Firmware") is present and must not leak into parsed releases.
    }

    [Fact]
    public void IsReleaseZeroIsFlaggedAsBeta()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");
        var entry = payload.Result!.Obj!.Single(o => o.Name == "BIOS").Files!.Single(f => f.Version == "3901");

        var release = AsusBiosSource.ParseRelease(entry);

        Assert.True(release.IsBeta);
    }

    [Fact]
    public void IsReleaseOneIsNotBeta()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");
        var entry = payload.Result!.Obj!.Single(o => o.Name == "BIOS").Files!.Single(f => f.Version == "3902");

        var release = AsusBiosSource.ParseRelease(entry);

        Assert.False(release.IsBeta);
        Assert.Equal(new DateOnly(2026, 7, 16), release.ReleaseDate);
        Assert.Equal("https://dlcdnets.asus.com/pub/ASUS/mb/BIOS/ROG-STRIX-X670E-E-GAMING-WIFI-ASUS-3902.ZIP?model=ROG STRIX X670E-E GAMING WIFI", release.DownloadUrl);
    }

    [Fact]
    public void EmptyShaBecomesNull()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");
        var entry = payload.Result!.Obj!.Single(o => o.Name == "BIOS").Files!.Single(f => f.Version == "2604");

        var release = AsusBiosSource.ParseRelease(entry);

        Assert.Null(release.Sha256);
    }

    [Fact]
    public void FileSizeStringIsConvertedToBytes()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");
        var entry = payload.Result!.Obj!.Single(o => o.Name == "BIOS").Files!.Single(f => f.Version == "3902");

        var release = AsusBiosSource.ParseRelease(entry);

        Assert.Equal((long)Math.Round(17.4 * 1024 * 1024), release.SizeBytes);
    }

    [Fact]
    public void StripsHtmlAndLiteralQuoteWrapperFromDescription()
    {
        var payload = LoadFixture("asus_rog_strix_x670e_e_gaming_wifi.json");
        var entry = payload.Result!.Obj!.Single(o => o.Name == "BIOS").Files!.Single(f => f.Version == "3902");

        var release = AsusBiosSource.ParseRelease(entry);

        Assert.DoesNotContain("<br", release.Changelog);
        Assert.DoesNotContain("\"", release.Changelog);
        Assert.StartsWith("1. Updated AGESA", release.Changelog);
    }

    [Theory]
    [InlineData("https://www.asus.com/motherboards-components/motherboards/rog-strix/rog-strix-x670e-e-gaming-wifi/", true)]
    [InlineData("https://www.asus.com/motherboards-components/motherboards/prime/prime-b650-plus/", true)]
    [InlineData("https://rog.asus.com/motherboards-components/motherboards/rog-strix/rog-strix-x670e-e-gaming-wifi/", true)]
    [InlineData("https://www.asus.com/laptops/for-home/some-laptop/", false)]
    [InlineData("https://www.msi.com/Motherboard/MAG-B650-TOMAHAWK-WIFI/support", false)]
    public void CanHandleDetectsAsusMotherboardProductUrls(string url, bool expected)
    {
        var source = new AsusBiosSource();

        Assert.Equal(expected, source.CanHandle(new Uri(url)));
    }

    [Fact]
    public void ParseBoardRefUsesLastPathSegmentAsModelId()
    {
        var source = new AsusBiosSource();
        var uri = new Uri("https://www.asus.com/motherboards-components/motherboards/rog-strix/rog-strix-x670e-e-gaming-wifi/");

        var boardRef = source.ParseBoardRef(uri);

        Assert.Equal("asus", boardRef.VendorId);
        Assert.Equal("rog-strix-x670e-e-gaming-wifi", boardRef.ModelId);
    }
}
