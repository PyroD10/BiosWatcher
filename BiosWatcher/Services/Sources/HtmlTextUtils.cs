using System.Text.RegularExpressions;

namespace BiosWatcher.Services.Sources;

/// <summary>Turns a vendor's HTML changelog snippet into plain text: shared by ASUS and ASRock, both of
/// which give changelog HTML with &lt;br/&gt; line breaks (Gigabyte's own cleanup lives in
/// GigabyteBiosSource itself since it also needs to walk sibling DOM nodes outside the changelog's own
/// &lt;ol&gt;, not just strip tags from one HTML string).</summary>
internal static partial class HtmlTextUtils
{
    public static string StripHtml(string html)
    {
        var withNewlines = BrTagRegex().Replace(html, "\n");
        var stripped = TagRegex().Replace(withNewlines, "");
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();
}
