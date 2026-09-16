using System.Globalization;
using System.Text.RegularExpressions;

namespace BiosWatcher.Services.Sources;

/// <summary>Converts vendor display strings like "17.4 MB" into bytes. Shared by ASUS and Gigabyte, which
/// (unlike MSI) only expose a human-readable size string rather than an exact byte count.</summary>
internal static partial class DownloadSizeParser
{
    public static long? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = SizeRegex().Match(text.Trim());
        if (!match.Success)
            return null;

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var multiplier = match.Groups[2].Value.ToUpperInvariant() switch
        {
            "KB" => 1024L,
            "MB" => 1024L * 1024,
            "GB" => 1024L * 1024 * 1024,
            _ => 1L,
        };

        return (long)Math.Round(value * multiplier);
    }

    [GeneratedRegex(@"^([\d.]+)\s*(KB|MB|GB)$", RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();
}
