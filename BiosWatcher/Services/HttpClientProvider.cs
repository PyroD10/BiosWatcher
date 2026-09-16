namespace BiosWatcher.Services;

/// <summary>Single shared HttpClient with a browser-like User-Agent — several vendor sites reject the default .NET UA.</summary>
public static class HttpClientProvider
{
    public static HttpClient Shared { get; } = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        return client;
    }
}
