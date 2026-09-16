using System.Globalization;
using System.Text.Json;
using BiosWatcher.Models;

namespace BiosWatcher.Services.Sources;

public class AsusBiosSource : IBiosSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string VendorId => "asus";

    public bool CanHandle(Uri supportUrl) =>
        supportUrl.Host.Contains("asus.com", StringComparison.OrdinalIgnoreCase) &&
        supportUrl.AbsolutePath.Contains("/motherboards-components/motherboards/", StringComparison.OrdinalIgnoreCase);

    public BoardRef ParseBoardRef(Uri supportUrl)
    {
        var modelId = supportUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrEmpty(modelId))
            throw new ArgumentException($"URL does not match an ASUS motherboard product page: {supportUrl}", nameof(supportUrl));

        return new BoardRef(VendorId, modelId, supportUrl.ToString());
    }

    public async Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default)
    {
        // www.asus.com's product.asmx/GetPDBIOS endpoint returns {"Status":"FAIL"} for every model as of
        // 2026-08-11 (verified live, both ROG and non-ROG); rog.asus.com's webapi works for both. See
        // CLAUDE.md's ASUS vendor section for the verification notes.
        var url = $"https://rog.asus.com/support/webapi/product/GetPDBIOS?website=global&model={Uri.EscapeDataString(boardRef.ModelId)}&pdid=0&cpu=";

        using var response = await HttpClientProvider.Shared.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<AsusSupportResponse>(stream, JsonOptions, cancellationToken);

        if (payload is null || !string.Equals(payload.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase) || payload.Result is null)
            throw new InvalidOperationException($"ASUS API returned an unexpected response for '{boardRef.ModelId}'.");

        // Multiple groups can be present (e.g. "Firmware" for USB/audio flashers) — only "BIOS" is the
        // EZ-Flash motherboard image; everything else is out of scope.
        var biosGroup = payload.Result.Obj?.FirstOrDefault(o => o.Name == "BIOS");
        var entries = biosGroup?.Files ?? [];
        var releases = entries.Select(ParseRelease).ToList();
        releases.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));

        var title = boardRef.ModelId.Replace('-', ' ').ToUpperInvariant();
        return new BoardInfo(title, releases);
    }

    internal static BiosRelease ParseRelease(AsusFile entry)
    {
        var version = (entry.Version ?? string.Empty).Trim();
        var isBeta = entry.IsRelease == "0";

        if (string.IsNullOrWhiteSpace(entry.ReleaseDate))
            throw new FormatException("ASUS download entry is missing a release date.");

        var releaseDate = DateOnly.ParseExact(entry.ReleaseDate.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture);

        var sha256 = string.IsNullOrWhiteSpace(entry.Sha256) ? null : entry.Sha256.Trim();

        return new BiosRelease(
            version,
            releaseDate,
            StripHtml(entry.Description ?? string.Empty),
            entry.DownloadUrl?.Global,
            DownloadSizeParser.Parse(entry.FileSize),
            sha256,
            isBeta);
    }

    /// <summary>Some ASUS entries wrap the whole description in a literal leading/trailing quote
    /// character as part of the content itself, on top of the usual HTML.</summary>
    internal static string StripHtml(string html) =>
        HtmlTextUtils.StripHtml(html).Trim('"').Trim();
}
