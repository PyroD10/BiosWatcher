using System.Diagnostics;
using BiosWatcher.Models;
using BiosWatcher.Services;
using BiosWatcher.Services.Sources;

namespace BiosWatcher.Forms;

public partial class MainForm : Form
{
    private static readonly Color NewRowBackColor = Color.FromArgb(255, 244, 200);

    private readonly ConfigRepository _configRepo = new();
    private readonly VendorRegistry _vendorRegistry = new([new MsiBiosSource(), new AsusBiosSource(), new GigabyteBiosSource(), new AsrockBiosSource(), new BiostarBiosSource()]);
    private readonly UpdateChecker _updateChecker;
    private readonly Font _newRowFont;

    private AppConfig _config = new();
    private bool _isExiting;
    private bool _isChecking;

    public MainForm()
    {
        InitializeComponent();

        _updateChecker = new UpdateChecker(_vendorRegistry);
        _newRowFont = new Font(boardsGrid.Font, FontStyle.Bold);
        emptyStateLabel.BringToFront();

        Load += (_, _) => LoadConfigAndRefresh();
        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
        boardsGrid.CellDoubleClick += BoardsGrid_CellDoubleClick;
        boardsGrid.SelectionChanged += (_, _) => UpdateButtonStates();

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;
        checkSelectedButton.Click += CheckSelectedButton_Click;
        checkAllButton.Click += CheckAllButton_Click;
        settingsButton.Click += SettingsButton_Click;

        contextCheckMenuItem.Click += CheckSelectedButton_Click;
        contextEditMenuItem.Click += EditButton_Click;
        contextDeleteMenuItem.Click += DeleteButton_Click;
        contextOpenMenuItem.Click += (_, _) => OpenSelectedSupportPage();

        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        trayIcon.BalloonTipClicked += (_, _) => RestoreFromTray();
        trayOpenMenuItem.Click += (_, _) => RestoreFromTray();
        trayCheckAllMenuItem.Click += async (_, _) => await RunFullCheckAsync();
        trayExitMenuItem.Click += (_, _) =>
        {
            _isExiting = true;
            Close();
        };

        periodicCheckTimer.Tick += async (_, _) => await RunFullCheckAsync();
    }

    private void LoadConfigAndRefresh()
    {
        _config = _configRepo.Load();
        RefreshGrid();
        RestartPeriodicCheckTimer();
    }

    private void RestartPeriodicCheckTimer()
    {
        var hours = Math.Max(1, _config.Settings.CheckIntervalHours);
        var intervalMs = Math.Min((long)hours * 60 * 60 * 1000, int.MaxValue);

        periodicCheckTimer.Stop();
        periodicCheckTimer.Interval = (int)intervalMs;
        periodicCheckTimer.Start();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            Hide();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isExiting || e.CloseReason != CloseReason.UserClosing)
            return;

        e.Cancel = true;
        Hide();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task RunFullCheckAsync()
    {
        if (_config.Boards.Count == 0 || _isChecking)
            return;

        _isChecking = true;
        var newVersionBoards = new List<Board>();

        SetBusy(true);
        try
        {
            await _updateChecker.CheckAllAsync(_config.Boards, _config.Settings, onBoardChecked: (board, isNew) =>
            {
                if (isNew)
                    newVersionBoards.Add(board);
                SaveConfig();
                RefreshGrid();
            });
        }
        finally
        {
            SetBusy(false);
            _isChecking = false;
        }

        if (newVersionBoards.Count > 0)
            ShowNewVersionBalloon(newVersionBoards);
    }

    private void ShowNewVersionBalloon(IReadOnlyList<Board> boards)
    {
        var title = boards.Count == 1 ? "New BIOS available" : $"New BIOS versions available ({boards.Count})";
        var text = string.Join(Environment.NewLine, boards.Select(b => $"{b.DisplayName}: {b.LastKnownVersion}"));

        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = text;
        trayIcon.ShowBalloonTip(10000);
    }

    private void RefreshGrid()
    {
        boardsGrid.Rows.Clear();

        foreach (var board in _config.Boards)
        {
            var isNew = board.IsNew(_config.Settings.NewFlagDays);

            var rowIndex = boardsGrid.Rows.Add(
                board.VendorId.ToUpperInvariant(),
                board.DisplayName,
                board.LastKnownVersion ?? "—",
                board.LastKnownReleaseDate is { } date ? date.ToString("dd.MM.yyyy") : "—",
                isNew ? "NEW" : "",
                board.LastChecked is { } checkedAt ? checkedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm") : "Never",
                board.LastCheckFailed ? "⚠ Failed" : "OK");

            var row = boardsGrid.Rows[rowIndex];
            row.Tag = board;

            if (isNew)
            {
                row.DefaultCellStyle.BackColor = NewRowBackColor;
                row.DefaultCellStyle.Font = _newRowFont;
            }

            if (board.LastCheckFailed)
            {
                row.Cells[statusColumn.Index].Style.ForeColor = Color.Firebrick;
                row.Cells[statusColumn.Index].ToolTipText = board.LastCheckError ?? "Check failed.";
            }
        }

        emptyStateLabel.Visible = _config.Boards.Count == 0;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        var hasSelection = GetSelectedBoard() is not null;
        editButton.Enabled = hasSelection;
        deleteButton.Enabled = hasSelection;
        checkSelectedButton.Enabled = hasSelection;
        contextCheckMenuItem.Enabled = hasSelection;
        contextEditMenuItem.Enabled = hasSelection;
        contextDeleteMenuItem.Enabled = hasSelection;
        contextOpenMenuItem.Enabled = hasSelection;
    }

    private Board? GetSelectedBoard()
    {
        if (boardsGrid.SelectedRows.Count == 0)
            return null;

        return boardsGrid.SelectedRows[0].Tag as Board;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new AddEditBoardForm(_vendorRegistry, _config.Boards, _config.Settings.IgnoreBetaVersions);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultBoard is not null)
        {
            _config.Boards.Add(dialog.ResultBoard);
            SaveConfig();
            RefreshGrid();
        }
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        var board = GetSelectedBoard();
        if (board is null)
            return;

        using var dialog = new AddEditBoardForm(board);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SaveConfig();
            RefreshGrid();
        }
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        var board = GetSelectedBoard();
        if (board is null)
            return;

        var result = MessageBox.Show(
            this,
            $"Remove \"{board.DisplayName}\" from the watch list?",
            "Delete board",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _config.Boards.Remove(board);
        SaveConfig();
        RefreshGrid();
    }

    private async void CheckSelectedButton_Click(object? sender, EventArgs e)
    {
        var board = GetSelectedBoard();
        if (board is null || _isChecking)
            return;

        _isChecking = true;
        SetBusy(true);
        bool isNewVersion;
        try
        {
            isNewVersion = await _updateChecker.CheckBoardAsync(board, _config.Settings);
            SaveConfig();
            RefreshGrid();
        }
        finally
        {
            SetBusy(false);
            _isChecking = false;
        }

        if (isNewVersion)
            ShowNewVersionBalloon([board]);
    }

    private async void CheckAllButton_Click(object? sender, EventArgs e) => await RunFullCheckAsync();

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SettingsForm(_config.Settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        SaveConfig();
        RefreshGrid();

        if (dialog.IntervalChanged)
            RestartPeriodicCheckTimer();
    }

    private void BoardsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        if (boardsGrid.Rows[e.RowIndex].Tag is not Board board)
            return;

        using var detail = new BoardDetailForm(board, _vendorRegistry);
        detail.ShowDialog(this);
    }

    private void OpenSelectedSupportPage()
    {
        var board = GetSelectedBoard();
        if (board is null)
            return;

        Process.Start(new ProcessStartInfo(board.SupportUrl) { UseShellExecute = true });
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        addButton.Enabled = !busy;
        checkAllButton.Enabled = !busy && _config.Boards.Count > 0;
        boardsGrid.Enabled = !busy;

        if (busy)
        {
            editButton.Enabled = false;
            deleteButton.Enabled = false;
            checkSelectedButton.Enabled = false;
        }
        else
        {
            UpdateButtonStates();
        }
    }

    private void SaveConfig()
    {
        try
        {
            _configRepo.Save(_config);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, $"Could not save config: {ex.Message}", "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
