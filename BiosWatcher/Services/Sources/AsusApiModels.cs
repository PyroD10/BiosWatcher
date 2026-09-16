using System.Text.Json.Serialization;

namespace BiosWatcher.Services.Sources;

internal sealed class AsusSupportResponse
{
    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("Result")]
    public AsusResult? Result { get; set; }
}

internal sealed class AsusResult
{
    [JsonPropertyName("Obj")]
    public List<AsusObjGroup>? Obj { get; set; }
}

internal sealed class AsusObjGroup
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Files")]
    public List<AsusFile>? Files { get; set; }
}

internal sealed class AsusFile
{
    [JsonPropertyName("Version")]
    public string? Version { get; set; }

    [JsonPropertyName("ReleaseDate")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("FileSize")]
    public string? FileSize { get; set; }

    [JsonPropertyName("DownloadUrl")]
    public AsusDownloadUrl? DownloadUrl { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    /// <summary>"0" for beta/preview builds, "1" for official releases — not documented by ASUS, inferred
    /// from real API responses (short-lived "0" versions superseded within days by an "1" version).</summary>
    [JsonPropertyName("IsRelease")]
    public string? IsRelease { get; set; }
}

internal sealed class AsusDownloadUrl
{
    [JsonPropertyName("Global")]
    public string? Global { get; set; }
}
