using System.Diagnostics;
using BiosWatcher.Models;
using BiosWatcher.Services;
using BiosWatcher.Services.Sources;

namespace BiosWatcher.Forms;

/// <summary>
/// Changelog/size/checksum aren't persisted in config.json (only the latest version+date are), so this
/// form always re-fetches from the vendor on open rather than reading anything from the Board.
/// </summary>
public partial class BoardDetailForm : Form
{
    private readonly Board _board;
    private readonly IBiosSource? _source;
    private IReadOnlyList<BiosRelease> _releases = [];

    public BoardDetailForm(Board board, VendorRegistry vendorRegistry)
    {
        _board = board;
        _source = vendorRegistry.FindByVendorId(board.VendorId);

        InitializeComponent();
        Text = $"{board.DisplayName} — BIOS details";
        titleLabel.Text = board.DisplayName;

        releasesListBox.SelectedIndexChanged += (_, _) => ShowSelectedRelease();
        downloadButton.Click += (_, _) => OpenSelectedDownload();
        openSupportPageButton.Click += (_, _) => OpenUrl(board.SupportUrl);
        Load += async (_, _) => await LoadReleasesAsync();
    }

    private async Task LoadReleasesAsync()
    {
        if (_source is null)
        {
            SetStatus($"Vendor '{_board.VendorId}' is not wired up yet.", isError: true);
            downloadButton.Enabled = false;
            return;
        }

        SetStatus("Fetching latest BIOS data…", isError: false);
        releasesListBox.Enabled = false;

        try
        {
            var boardRef = new BoardRef(_board.VendorId, _board.ModelId, _board.SupportUrl);
            var info = await _source.GetBoardInfoAsync(boardRef);
            _releases = info.Releases;

            releasesListBox.Items.Clear();
            foreach (var release in _releases)
            {
                releasesListBox.Items.Add(FormatReleaseLabel(release));
            }

            SetStatus(_releases.Count == 0 ? "No BIOS releases found." : "", isError: false);

            if (releasesListBox.Items.Count > 0)
                releasesListBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load BIOS details: {ex.Message}", isError: true);
            downloadButton.Enabled = false;
        }
        finally
        {
            releasesListBox.Enabled = true;
        }
    }

    private static string FormatReleaseLabel(BiosRelease release)
    {
        var betaTag = release.IsBeta ? " (Beta)" : "";
        return $"{release.Version}{betaTag} — {release.ReleaseDate:dd.MM.yyyy}";
    }

    private void ShowSelectedRelease()
    {
        var index = releasesListBox.SelectedIndex;
        if (index < 0 || index >= _releases.Count)
        {
            changelogTextBox.Text = "";
            sizeValueLabel.Text = "";
            shaTextBox.Text = "";
            downloadButton.Enabled = false;
            return;
        }

        var release = _releases[index];
        changelogTextBox.Text = release.Changelog;
        sizeValueLabel.Text = FormatSize(release.SizeBytes);
        shaTextBox.Text = release.Sha256 ?? "(not provided)";
        downloadButton.Enabled = release.DownloadUrl is not null;
    }

    private static string FormatSize(long? bytes)
    {
        if (bytes is null)
            return "(unknown)";

        var mb = bytes.Value / 1024.0 / 1024.0;
        return $"{mb:0.0} MB";
    }

    private void OpenSelectedDownload()
    {
        var index = releasesListBox.SelectedIndex;
        if (index < 0 || index >= _releases.Count)
            return;

        var url = _releases[index].DownloadUrl;
        if (url is not null)
            OpenUrl(url);
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private void SetStatus(string message, bool isError)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = isError ? Color.Firebrick : SystemColors.ControlText;
    }
}
