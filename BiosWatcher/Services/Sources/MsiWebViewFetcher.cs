using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BiosWatcher.Services.Sources;

/// <summary>
/// msi.com sits behind an Akamai WAF that blocks plain HttpClient/curl requests outright — confirmed
/// (2026-08-12) both from the dev sandbox and from a real user's home machine, on the static product page
/// *and* the JSON API, even with a full set of spoofed Chrome headers (User-Agent, Referer, sec-ch-ua,
/// Accept, ...). Since the block reproduced identically on two unrelated networks, it isn't IP/datacenter
/// reputation — it's almost certainly TLS/HTTP client fingerprinting (a real Chrome TLS handshake looks
/// nothing like .NET SocketsHttpHandler's, no matter what headers ride on top of it), which no amount of
/// header spoofing can fix.
///
/// The actual fix is to stop pretending to be a browser and just use one: this hosts a real Chromium
/// engine (Microsoft Edge WebView2, same engine the user's own browser uses) off-screen, navigates it
/// directly to the target URL, and captures the raw response body via
/// <see cref="CoreWebView2.WebResourceResponseReceived"/> — bypassing Edge's built-in JSON pretty-printer
/// UI entirely, since that event exposes the exact bytes the server sent, not the rendered DOM.
/// </summary>
internal sealed class MsiWebViewFetcher
{
    private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds(20);

    private Form? _hostForm;
    private WebView2? _webView;
    private Task? _initTask;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    public async Task<(int StatusCode, string Body)> FetchAsync(string url, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            var tcs = new TaskCompletionSource<(int, string)>(TaskCreationOptions.RunContinuationsAsynchronously);

            async void OnResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
            {
                if (!string.Equals(e.Request.Uri, url, StringComparison.OrdinalIgnoreCase))
                    return;

                try
                {
                    await using var stream = await e.Response.GetContentAsync();
                    using var reader = new StreamReader(stream);
                    var body = await reader.ReadToEndAsync();
                    tcs.TrySetResult((e.Response.StatusCode, body));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            var core = _webView!.CoreWebView2;
            core.WebResourceResponseReceived += OnResponseReceived;
            try
            {
                core.Navigate(url);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(NavigationTimeout);
                await using var registration = timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token));

                return await tcs.Task;
            }
            finally
            {
                core.WebResourceResponseReceived -= OnResponseReceived;
            }
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        return _initTask ??= InitializeAsync(cancellationToken);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Off-screen (not just invisible): WebView2 needs a real HWND with a message loop to run its
        // Chromium child process against, so it can't be built with zero window at all — a 1x1 window
        // parked off the visible desktop area is the standard headless-WebView2 pattern.
        _hostForm = new Form
        {
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-2000, -2000),
            Size = new Size(1, 1),
        };
        _webView = new WebView2 { Dock = DockStyle.Fill };
        _hostForm.Controls.Add(_webView);
        _hostForm.Show();

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiosWatcher", "WebView2");

        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await _webView.EnsureCoreWebView2Async(environment);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "MSI requires the Microsoft Edge WebView2 Runtime to bypass msi.com's bot protection, and it " +
                "could not be initialized (it's usually pre-installed with Edge on Windows 10/11; if missing, " +
                "install the \"Evergreen\" WebView2 Runtime from Microsoft). " + ex.Message, ex);
        }
    }
}
