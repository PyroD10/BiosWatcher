using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BiosWatcher.Models;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Services.Sources;

/// <summary>
/// Verified live (2026-08-12) directly against biostar.com.tw (this sandbox reaches it fine with the
/// shared browser UA — no WAF block like msi.com/gigabyte.com/asrock.com hit). A product page (e.g.
/// <c>/app/en/mb/introduction.php?S_ID=1079</c>) has a "DOWNLOAD" tab that AJAX-loads a sibling
/// <c>mb_download.php?S_ID=&lt;id&gt;</c> fragment (server-rendered HTML, no JSON API) — same
/// AJAX-fragment shape as ASRock's BIOS.html, discovered the same way: reading the page's own tab-switch
/// JS rather than guessing. The fragment holds every download category (Manual, BIOS, chipset/LAN/audio
/// drivers, utilities) as sibling "tab-box" sections that all share identical cell markup, so the BIOS
/// section must be selected by its tab-title text, not assumed to be "the" table.
/// </summary>
public partial class BiostarBiosSource : IBiosSource
{
    public string VendorId => "biostar";

    public bool CanHandle(Uri supportUrl) =>
        supportUrl.Host.Contains("biostar.com.tw", StringComparison.OrdinalIgnoreCase) &&
        IntroductionPathRegex().IsMatch(supportUrl.AbsolutePath) &&
        ModelIdRegex().IsMatch(supportUrl.Query);

    public BoardRef ParseBoardRef(Uri supportUrl)
    {
        var match = ModelIdRegex().Match(supportUrl.Query);
        if (!IntroductionPathRegex().IsMatch(supportUrl.AbsolutePath) || !match.Success)
            throw new ArgumentException($"URL does not match a Biostar motherboard product page: {supportUrl}", nameof(supportUrl));

        return new BoardRef(VendorId, match.Groups[1].Value, supportUrl.ToString());
    }

    public async Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default)
    {
        // Always fetch the /app/en/ (English) pages for consistency, regardless of what locale segment
        // (if any) the pasted URL used — same reasoning as GigabyteBiosSource always using the global page.
        var introDoc = await FetchHtmlAsync(
            $"https://www.biostar.com.tw/app/en/mb/introduction.php?S_ID={boardRef.ModelId}", cancellationToken);
        var downloadDoc = await FetchHtmlAsync(
            $"https://www.biostar.com.tw/app/en/mb/mb_download.php?S_ID={boardRef.ModelId}", cancellationToken);

        return new BoardInfo(ParseTitle(introDoc, boardRef.ModelId), ParseReleases(downloadDoc));
    }

    private static async Task<HtmlDocument> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await HttpClientProvider.Shared.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    internal static string ParseTitle(HtmlDocument doc, string fallback)
    {
        var text = doc.DocumentNode.SelectSingleNode("//div[@class='info-text']/div[@class='main']/p")?.InnerText;
        return string.IsNullOrWhiteSpace(text) ? fallback : NormalizeWhitespace(text);
    }

    /// <summary>Selects the "BIOS" tab-box by its title text, since every download category (Manual,
    /// BIOS, drivers, utilities) reuses the exact same tab-box/table/tr/td markup with no distinguishing
    /// class of its own.</summary>
    internal static List<BiosRelease> ParseReleases(HtmlDocument doc)
    {
        var biosBox = doc.DocumentNode.SelectSingleNode(
            "//div[@class='tab-box'][.//div[@class='tab-title']/p[normalize-space(text())='BIOS']]");
        if (biosBox is null)
            return [];

        var rows = biosBox.SelectNodes(".//div[@class='tbody']/div[@class='tr']");
        if (rows is null)
            return [];

        // The real BIOS tab also carries two trailing static "how to update" guide-PDF rows
        // (a Smart-BIOS-update SOP and a manual) using rwd-title="Title"/"System" instead of
        // "Version"/"Description" — not releases. Filter by the Version cell's presence rather than
        // assuming every row in the section is a real entry.
        var releases = rows
            .Where(row => row.SelectSingleNode(".//div[@rwd-title='Version']") is not null)
            .Select(ParseRow)
            .ToList();

        releases.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));
        return releases;
    }

    internal static BiosRelease ParseRow(HtmlNode row)
    {
        var version = CellText(row, "Version");
        var dateText = CellText(row, "Date");
        var releaseDate = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var descNode = row.SelectSingleNode(".//div[@rwd-title='Description']");
        var changelog = descNode is null ? string.Empty : HtmlTextUtils.StripHtml(descNode.InnerHtml);

        var sizeText = CellText(row, "File Size");
        var downloadUrl = ExtractDownloadUrl(row);

        // Biostar never labels a BIOS release as beta on any product page sampled (two boards checked,
        // AMD and Intel) — no version suffix, no separate flag. There's also no SHA256/checksum field
        // anywhere in the download fragment, unlike MSI/ASUS/ASRock.
        return new BiosRelease(version, releaseDate, changelog, downloadUrl, DownloadSizeParser.Parse(sizeText), Sha256: null, IsBeta: false);
    }

    /// <summary>The download link isn't a real href — it's <c>onclick="openLightboxWithParameters(id,
    /// bstName, zipName, ...)"</c> driving a JS lightbox that fills in <c>../../../upload/Bios/&lt;name&gt;</c>
    /// at click time. The real, verified absolute URL (confirmed with a live HEAD request) is
    /// <c>https://www.biostar.com.tw/upload/Bios/&lt;zipName&gt;</c> — extracted here by regex against the
    /// onclick text instead of an href attribute that doesn't exist.</summary>
    private static string? ExtractDownloadUrl(HtmlNode row)
    {
        var onclick = row.SelectSingleNode(".//div[@rwd-title='Download']//a")?.Attributes["onclick"]?.Value;
        if (onclick is null)
            return null;

        var match = DownloadFileNameRegex().Match(onclick);
        return match.Success ? $"https://www.biostar.com.tw/upload/Bios/{match.Groups[1].Value}" : null;
    }

    private static string CellText(HtmlNode row, string rwdTitle)
    {
        var node = row.SelectSingleNode($".//div[@rwd-title='{rwdTitle}']/p");
        return node is null ? string.Empty : NormalizeWhitespace(node.InnerText);
    }

    private static string NormalizeWhitespace(string text) =>
        WhitespaceRegex().Replace(WebUtility.HtmlDecode(text), " ").Trim();

    [GeneratedRegex(@"/mb/introduction\.php$", RegexOptions.IgnoreCase)]
    private static partial Regex IntroductionPathRegex();

    [GeneratedRegex(@"[?&]S_ID=(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ModelIdRegex();

    [GeneratedRegex(@"openLightboxWithParameters\(\s*\d+\s*,\s*'[^']*'\s*,\s*'([^']*)'", RegexOptions.IgnoreCase)]
    private static partial Regex DownloadFileNameRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
