using System.Globalization;
using System.Text.RegularExpressions;
using BiosWatcher.Models;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Services.Sources;

/// <summary>
/// Discovery (2026-08-11): asrock.com itself 403s under Incapsula from the dev sandbox — see CLAUDE.md's
/// ASRock vendor section — but the real BIOS fragment markup was obtained from a live browser session
/// (DevTools → Network → the request the product page's own <c>$('#BIOS').load('BIOS.html')</c> call
/// makes) and pasted in directly, so this is built against genuine current markup, not a guess or an
/// archive. <c>BiosWatcher.Tests/Fixtures/asrock_x670e_taichi_bios.html</c> is a trimmed copy of that
/// real fragment.
/// </summary>
public partial class AsrockBiosSource : IBiosSource
{
    private static readonly Regex Sha256Regex = new(@"SHA256:\s*([0-9a-fA-F]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public string VendorId => "asrock";

    public bool CanHandle(Uri supportUrl) =>
        supportUrl.Host.Contains("asrock.com", StringComparison.OrdinalIgnoreCase) &&
        SlugRegex().IsMatch(supportUrl.AbsolutePath);

    public BoardRef ParseBoardRef(Uri supportUrl)
    {
        var match = SlugRegex().Match(supportUrl.AbsolutePath);
        if (!match.Success)
            throw new ArgumentException($"URL does not match an ASRock motherboard product page: {supportUrl}", nameof(supportUrl));

        var modelId = Uri.UnescapeDataString(match.Groups[1].Value);
        return new BoardRef(VendorId, modelId, supportUrl.ToString());
    }

    public async Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default)
    {
        // The BIOS list loads via a sibling "BIOS.html" AJAX fragment next to the product's own
        // index.asp, discovered from the real $('#BIOS').load('BIOS.html') call — see the class remarks.
        var lastSlash = boardRef.SupportUrl.LastIndexOf('/');
        var url = boardRef.SupportUrl[..(lastSlash + 1)] + "BIOS.html";

        using var response = await HttpClientProvider.Shared.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return new BoardInfo(boardRef.ModelId, ParseReleases(doc));
    }

    internal static List<BiosRelease> ParseReleases(HtmlDocument doc)
    {
        var rows = doc.DocumentNode.SelectNodes("//table/tbody/tr");
        if (rows is null)
            return [];

        var releases = rows.Select(ParseRow).ToList();
        releases.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));
        return releases;
    }

    /// <summary>The table has no CSS classes to hook into — columns are Version, Date, Size, Update
    /// method (skipped — Instant Flash / Flashback links, not release data), Description, Global
    /// download, China download (FTP — skipped in favor of the HTTPS Global link).</summary>
    internal static BiosRelease ParseRow(HtmlNode row)
    {
        var cells = row.SelectNodes("td");
        if (cells is null || cells.Count < 7)
            throw new FormatException("ASRock BIOS row does not have the expected column count.");

        var versionText = NormalizeWhitespace(cells[0].InnerText);
        var isBeta = versionText.Contains("[Beta]", StringComparison.OrdinalIgnoreCase);
        var version = versionText.Replace("[Beta]", "", StringComparison.OrdinalIgnoreCase).Trim();

        var dateText = NormalizeWhitespace(cells[1].InnerText);
        var releaseDate = DateOnly.ParseExact(dateText, "yyyy/M/d", CultureInfo.InvariantCulture);

        var sizeText = NormalizeWhitespace(cells[2].InnerText);

        // SHA256 lives *inside* the description cell as a trailing "SHA256: <hash>" line, not its own
        // column — split it out and keep everything before it as the changelog.
        var descText = HtmlTextUtils.StripHtml(cells[4].InnerHtml);
        var shaMatch = Sha256Regex.Match(descText);
        var sha256 = shaMatch.Success ? shaMatch.Groups[1].Value : null;
        var changelog = (shaMatch.Success ? descText[..shaMatch.Index] : descText).Trim();

        var downloadUrl = cells[5].SelectSingleNode(".//a[@href]")?.Attributes["href"]?.Value;

        return new BiosRelease(version, releaseDate, changelog, downloadUrl, DownloadSizeParser.Parse(sizeText), sha256, isBeta);
    }

    private static string NormalizeWhitespace(string text) => WhitespaceRegex.Replace(text, " ").Trim();

    [GeneratedRegex(@"/mb/[^/]+/([^/]+)/index\.asp", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();
}
