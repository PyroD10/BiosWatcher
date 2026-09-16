using BiosWatcher.Models;
using BiosWatcher.Services;

namespace BiosWatcher.Forms;

/// <summary>Add mode fetches a board from its support URL; Edit mode only renames/re-points an existing board.</summary>
public partial class AddEditBoardForm : Form
{
    private readonly VendorRegistry? _vendorRegistry;
    private readonly IReadOnlyList<Board> _existingBoards;
    private readonly bool _ignoreBetaVersions;
    private readonly Board? _editingBoard;

    public Board? ResultBoard { get; private set; }

    public AddEditBoardForm(VendorRegistry vendorRegistry, IReadOnlyList<Board> existingBoards, bool ignoreBetaVersions)
    {
        _vendorRegistry = vendorRegistry;
        _existingBoards = existingBoards;
        _ignoreBetaVersions = ignoreBetaVersions;

        InitializeComponent();
        Text = "Add board";
        okButton.Text = "Add";
        displayNameLabel.Visible = false;
        displayNameTextBox.Visible = false;

        okButton.Click += async (_, _) => await AddBoardAsync();
    }

    public AddEditBoardForm(Board boardToEdit)
    {
        _existingBoards = [];
        _editingBoard = boardToEdit;

        InitializeComponent();
        Text = "Edit board";
        okButton.Text = "Save";
        urlLabel.Text = "Support URL:";
        urlTextBox.Text = boardToEdit.SupportUrl;
        displayNameTextBox.Text = boardToEdit.DisplayName;

        okButton.Click += (_, _) => SaveEdit();
    }

    private void SaveEdit()
    {
        var url = urlTextBox.Text.Trim();
        var name = displayNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Display name cannot be empty.");
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            ShowError("Enter a valid URL.");
            return;
        }

        _editingBoard!.DisplayName = name;
        _editingBoard.SupportUrl = url;
        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task AddBoardAsync()
    {
        var urlText = urlTextBox.Text.Trim();
        if (!Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
        {
            ShowError("Enter a valid URL.");
            return;
        }

        var source = _vendorRegistry!.FindSource(uri);
        if (source is null)
        {
            ShowError(
                "Unknown vendor. Supported URL formats:\n" +
                "MSI: https://www.msi.com/Motherboard/<MODEL>/support\n" +
                "ASUS: https://www.asus.com/motherboards-components/motherboards/<series>/<model>/\n" +
                "Gigabyte: https://www.gigabyte.com/Motherboard/<MODEL>-rev-<REV>/support\n" +
                "ASRock: https://www.asrock.com/mb/<PLATFORM>/<MODEL>/index.asp\n" +
                "Biostar: https://www.biostar.com.tw/app/en/mb/introduction.php?S_ID=<ID>");
            return;
        }

        BoardRef boardRef;
        try
        {
            boardRef = source.ParseBoardRef(uri);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
            return;
        }

        if (_existingBoards.Any(b => b.VendorId == boardRef.VendorId && b.ModelId == boardRef.ModelId))
        {
            ShowError("This board is already in your list.");
            return;
        }

        SetBusy(true);
        try
        {
            var info = await source.GetBoardInfoAsync(boardRef);
            var latest = UpdateChecker.SelectLatestRelease(info.Releases, _ignoreBetaVersions);

            ResultBoard = new Board
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = boardRef.VendorId,
                ModelId = boardRef.ModelId,
                DisplayName = info.Title,
                SupportUrl = boardRef.SupportUrl,
                LastKnownVersion = latest?.Version,
                LastKnownReleaseDate = latest?.ReleaseDate,
                LastChecked = DateTimeOffset.UtcNow,
                LastCheckFailed = false,
                NotifiedVersions = latest is not null ? [latest.Version] : [],
            };

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"Could not fetch BIOS info: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        urlTextBox.Enabled = !busy;
        okButton.Enabled = !busy;
        cancelButton.Enabled = !busy;

        // Only the "busy" transition touches the status label — clearing it unconditionally on the
        // "not busy" transition would wipe out an error ShowError just set in the catch block above it.
        if (busy)
        {
            statusLabel.ForeColor = SystemColors.ControlText;
            statusLabel.Text = "Fetching BIOS info…";
        }
    }

    private void ShowError(string message)
    {
        statusLabel.ForeColor = Color.Firebrick;
        statusLabel.Text = message;
    }
}
