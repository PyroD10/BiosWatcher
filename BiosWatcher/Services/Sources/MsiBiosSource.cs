using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BiosWatcher.Models;

namespace BiosWatcher.Services.Sources;

public partial class MsiBiosSource : IBiosSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MsiWebViewFetcher _fetcher = new();

    public string VendorId => "msi";

    public bool CanHandle(Uri supportUrl) =>
        supportUrl.Host.Contains("msi.com", StringComparison.OrdinalIgnoreCase) &&
        SlugRegex().IsMatch(supportUrl.AbsolutePath);

    public BoardRef ParseBoardRef(Uri supportUrl)
    {
        var match = SlugRegex().Match(supportUrl.AbsolutePath);
        if (!match.Success)
            throw new ArgumentException($"URL does not match an MSI motherboard support page: {supportUrl}", nameof(supportUrl));

        return new BoardRef(VendorId, match.Groups[1].Value, supportUrl.ToString());
    }

    public async Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default)
    {
        var url = $"https://www.msi.com/api/v1/product/support/panel?product={Uri.EscapeDataString(boardRef.ModelId)}&type=bios";

        // Plain HttpClient/curl gets a flat 403 from msi.com's Akamai WAF, confirmed even with a full set
        // of spoofed browser headers — see MsiWebViewFetcher's remarks. Route through a real Chromium
        // engine instead, which passes the same bot check a real browser would.
        var (statusCode, body) = await _fetcher.FetchAsync(url, cancellationToken);
        if (statusCode is < 200 or >= 300)
            throw new HttpRequestException($"MSI returned HTTP {statusCode} for '{boardRef.ModelId}'.");

        var payload = JsonSerializer.Deserialize<MsiSupportResponse>(body, JsonOptions);

        if (payload?.Status?.Code != 200 || payload.Result is null)
            throw new InvalidOperationException($"MSI API returned an unexpected response for '{boardRef.ModelId}'.");

        var entries = payload.Result.Downloads?.AmiBios ?? [];
        var releases = entries.Select(ParseRelease).ToList();
        releases.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));

        return new BoardInfo(payload.Result.Title ?? boardRef.ModelId, releases);
    }

    internal static BiosRelease ParseRelease(MsiDownloadEntry entry)
    {
        var rawVersion = entry.Version ?? string.Empty;
        var isBeta = rawVersion.Contains("Beta", StringComparison.OrdinalIgnoreCase);
        var version = BetaSuffixRegex().Replace(rawVersion, "").Trim();

        if (string.IsNullOrWhiteSpace(entry.ReleaseDate))
            throw new FormatException("MSI download entry is missing a release date.");

        var releaseDate = DateOnly.ParseExact(entry.ReleaseDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var sha256 = entry.Sha256;
        if (!string.IsNullOrEmpty(sha256))
        {
            sha256 = sha256
                .Replace("SHA-256:", "", StringComparison.OrdinalIgnoreCase)
                .Replace("<br>", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        return new BiosRelease(
            version,
            releaseDate,
            entry.Description ?? string.Empty,
            entry.DownloadUrl,
            entry.DownloadSize,
            string.IsNullOrEmpty(sha256) ? null : sha256,
            isBeta);
    }

    [GeneratedRegex(@"Motherboard/([^/]+)/support", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"\s*\(Beta version\)\s*", RegexOptions.IgnoreCase)]
    private static partial Regex BetaSuffixRegex();
}
