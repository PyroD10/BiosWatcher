using System.Text.Json.Serialization;

namespace BiosWatcher.Services.Sources;

internal sealed class MsiSupportResponse
{
    [JsonPropertyName("status")]
    public MsiStatus? Status { get; set; }

    [JsonPropertyName("result")]
    public MsiResult? Result { get; set; }
}

internal sealed class MsiStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
}

internal sealed class MsiResult
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("downloads")]
    public MsiDownloads? Downloads { get; set; }
}

internal sealed class MsiDownloads
{
    [JsonPropertyName("AMI BIOS")]
    public List<MsiDownloadEntry>? AmiBios { get; set; }
}

internal sealed class MsiDownloadEntry
{
    [JsonPropertyName("download_version")]
    public string? Version { get; set; }

    [JsonPropertyName("download_release")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("download_description")]
    public string? Description { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("download_size")]
    public long? DownloadSize { get; set; }

    [JsonPropertyName("download_sha256")]
    public string? Sha256 { get; set; }
}
