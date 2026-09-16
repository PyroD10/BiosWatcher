using System.Globalization;
using System.Text.RegularExpressions;
using BiosWatcher.Models;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BiosWatcher.Services.Sources;

public partial class GigabyteBiosSource : IBiosSource
{
    private static readonly string[] DateFormats = ["MMM dd, yyyy", "MMM d, yyyy"];

    public string VendorId => "gigabyte";

    public bool CanHandle(Uri supportUrl) =>
        supportUrl.Host.Contains("gigabyte.com", StringComparison.OrdinalIgnoreCase) &&
        SlugRegex().IsMatch(supportUrl.AbsolutePath);

    public BoardRef ParseBoardRef(Uri supportUrl)
    {
        var match = SlugRegex().Match(supportUrl.AbsolutePath);
        if (!match.Success)
            throw new ArgumentException($"URL does not match a Gigabyte motherboard support page: {supportUrl}", nameof(supportUrl));

        return new BoardRef(VendorId, match.Groups[1].Value, supportUrl.ToString());
    }

    public async Task<BoardInfo> GetBoardInfoAsync(BoardRef boardRef, CancellationToken cancellationToken = default)
    {
        // Always fetch the global (locale-free) page for consistency, regardless of what locale segment
        // (if any) the pasted URL used.
        var url = $"https://www.gigabyte.com/Motherboard/{boardRef.ModelId}/support";

        using var response = await HttpClientProvider.Shared.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return new BoardInfo(ParseTitle(doc, boardRef.ModelId), ParseReleases(doc));
    }

    internal static string ParseTitle(HtmlDocument doc, string fallback)
    {
        var text = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'pageTitle')]")?.InnerText;
        return string.IsNullOrWhiteSpace(text) ? fallback : NormalizeWhitespace(text);
    }

    internal static List<BiosRelease> ParseReleases(HtmlDocument doc)
    {
        // BIOS rows share cell classes (download-version, download-date, ...) with the table's own header
        // row, so the row itself must be matched on "div-table-body-BIOS", not just a cell class.
        var rows = doc.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' div-table-body-BIOS ')]");

        if (rows is null)
            return [];

        var releases = rows.Select(ParseRow).ToList();
        releases.Sort((a, b) => b.ReleaseDate.CompareTo(a.ReleaseDate));
        return releases;
    }

    internal static BiosRelease ParseRow(HtmlNode row)
    {
        var version = CellText(row, "download-version");
        var dateText = CellText(row, "download-date");
        var sizeText = CellText(row, "download-size");
        var descNode = row.SelectSingleNode(".//div[contains(@class,'download-desc')]");
        var downloadUrl = row.SelectSingleNode(".//div[contains(@class,'download-site')]//a[@href]")?.Attributes["href"]?.Value;

        if (!DateOnly.TryParseExact(dateText, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
            throw new FormatException($"Gigabyte BIOS row has an unparseable date: '{dateText}'.");

        var (checksum, changelog) = ParseDescription(descNode);

        return new BiosRelease(
            version,
            releaseDate,
            changelog,
            downloadUrl,
            DownloadSizeParser.Parse(sizeText),
            checksum,
            IsBeta: false); // Gigabyte doesn't label betas explicitly (letter-suffix versions aren't reliable) — never auto-classify.
    }

    /// <summary>The description list's first &lt;li&gt; is a "Checksum : XXXX" line (note the space before
    /// the colon) — pull it out into the Sha256 slot. Footnotes referenced from a bullet (e.g. "*") often
    /// sit as plain text or a trailing &lt;p&gt;/&lt;div&gt; *outside* the &lt;ol&gt;, not as another
    /// &lt;li&gt; — those are appended to the changelog too so they aren't silently dropped.</summary>
    private static (string? Checksum, string Changelog) ParseDescription(HtmlNode? descNode)
    {
        if (descNode is null)
            return (null, string.Empty);

        string? checksum = null;
        var lines = new List<string>();

        foreach (var li in descNode.SelectNodes(".//li") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = NormalizeWhitespace(li.InnerText);
            var match = checksum is null ? ChecksumRegex().Match(text) : Match.Empty;
            if (match.Success)
                checksum = match.Groups[1].Value;
            else
                lines.Add(text);
        }

        var olNode = descNode.SelectSingleNode(".//ol");
        foreach (var child in descNode.ChildNodes)
        {
            if (child == olNode)
                continue;

            var text = NormalizeWhitespace(child.InnerText);
            if (text.Length > 0)
                lines.Add(text);
        }

        return (checksum, string.Join(Environment.NewLine, lines));
    }

    private static string CellText(HtmlNode row, string cellClass)
    {
        var node = row.SelectSingleNode($".//div[contains(@class,'{cellClass}')]");
        return node is null ? string.Empty : NormalizeWhitespace(node.InnerText);
    }

    private static string NormalizeWhitespace(string text) =>
        WhitespaceRegex().Replace(HtmlEntity.DeEntitize(text) ?? text, " ").Trim();

    [GeneratedRegex(@"Motherboard/([^/]+)/support", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"Checksum\s*:\s*(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ChecksumRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
