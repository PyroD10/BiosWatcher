namespace BiosWatcher.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private ToolStrip toolStrip;
    private ToolStripButton addButton;
    private ToolStripButton editButton;
    private ToolStripButton deleteButton;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripButton checkSelectedButton;
    private ToolStripButton checkAllButton;
    private ToolStripSeparator toolStripSeparator2;
    private ToolStripButton settingsButton;
    private DataGridView boardsGrid;
    private Label emptyStateLabel;
    private DataGridViewTextBoxColumn vendorColumn;
    private DataGridViewTextBoxColumn nameColumn;
    private DataGridViewTextBoxColumn latestVersionColumn;
    private DataGridViewTextBoxColumn releaseDateColumn;
    private DataGridViewTextBoxColumn newColumn;
    private DataGridViewTextBoxColumn lastCheckedColumn;
    private DataGridViewTextBoxColumn statusColumn;
    private ContextMenuStrip boardsContextMenu;
    private ToolStripMenuItem contextCheckMenuItem;
    private ToolStripMenuItem contextEditMenuItem;
    private ToolStripMenuItem contextDeleteMenuItem;
    private ToolStripMenuItem contextOpenMenuItem;
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayContextMenu;
    private ToolStripMenuItem trayOpenMenuItem;
    private ToolStripMenuItem trayCheckAllMenuItem;
    private ToolStripSeparator trayContextSeparator;
    private ToolStripMenuItem trayExitMenuItem;
    private System.Windows.Forms.Timer periodicCheckTimer;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        toolStrip = new ToolStrip();
        addButton = new ToolStripButton();
        editButton = new ToolStripButton();
        deleteButton = new ToolStripButton();
        toolStripSeparator1 = new ToolStripSeparator();
        checkSelectedButton = new ToolStripButton();
        checkAllButton = new ToolStripButton();
        toolStripSeparator2 = new ToolStripSeparator();
        settingsButton = new ToolStripButton();
        boardsGrid = new DataGridView();
        emptyStateLabel = new Label();
        vendorColumn = new DataGridViewTextBoxColumn();
        nameColumn = new DataGridViewTextBoxColumn();
        latestVersionColumn = new DataGridViewTextBoxColumn();
        releaseDateColumn = new DataGridViewTextBoxColumn();
        newColumn = new DataGridViewTextBoxColumn();
        lastCheckedColumn = new DataGridViewTextBoxColumn();
        statusColumn = new DataGridViewTextBoxColumn();
        boardsContextMenu = new ContextMenuStrip(components);
        contextCheckMenuItem = new ToolStripMenuItem();
        contextEditMenuItem = new ToolStripMenuItem();
        contextDeleteMenuItem = new ToolStripMenuItem();
        contextOpenMenuItem = new ToolStripMenuItem();
        trayIcon = new NotifyIcon(components);
        trayContextMenu = new ContextMenuStrip(components);
        trayOpenMenuItem = new ToolStripMenuItem();
        trayCheckAllMenuItem = new ToolStripMenuItem();
        trayContextSeparator = new ToolStripSeparator();
        trayExitMenuItem = new ToolStripMenuItem();
        periodicCheckTimer = new System.Windows.Forms.Timer(components);
        toolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)boardsGrid).BeginInit();
        boardsContextMenu.SuspendLayout();
        SuspendLayout();
        //
        // toolStrip
        //
        toolStrip.Items.AddRange(new ToolStripItem[] {
            addButton, editButton, deleteButton, toolStripSeparator1, checkSelectedButton, checkAllButton, toolStripSeparator2, settingsButton });
        toolStrip.Location = new Point(0, 0);
        toolStrip.Name = "toolStrip";
        toolStrip.Size = new Size(900, 25);
        toolStrip.TabIndex = 0;
        //
        // addButton
        //
        addButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        addButton.Name = "addButton";
        addButton.Text = "Add board…";
        //
        // editButton
        //
        editButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        editButton.Enabled = false;
        editButton.Name = "editButton";
        editButton.Text = "Edit";
        //
        // deleteButton
        //
        deleteButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        deleteButton.Enabled = false;
        deleteButton.Name = "deleteButton";
        deleteButton.Text = "Delete";
        //
        // checkSelectedButton
        //
        checkSelectedButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        checkSelectedButton.Enabled = false;
        checkSelectedButton.Name = "checkSelectedButton";
        checkSelectedButton.Text = "Check this board";
        //
        // checkAllButton
        //
        checkAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        checkAllButton.Name = "checkAllButton";
        checkAllButton.Text = "Check all";
        //
        // settingsButton
        //
        settingsButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        settingsButton.Name = "settingsButton";
        settingsButton.Text = "Settings…";
        //
        // emptyStateLabel
        //
        emptyStateLabel.Anchor = AnchorStyles.None;
        emptyStateLabel.AutoSize = true;
        emptyStateLabel.ForeColor = SystemColors.GrayText;
        emptyStateLabel.Location = new Point(310, 240);
        emptyStateLabel.Name = "emptyStateLabel";
        emptyStateLabel.Size = new Size(280, 15);
        emptyStateLabel.Text = "No boards yet. Click \"Add board…\" to get started.";
        emptyStateLabel.Visible = false;
        //
        // boardsGrid
        //
        boardsGrid.AllowUserToAddRows = false;
        boardsGrid.AllowUserToDeleteRows = false;
        boardsGrid.AllowUserToResizeRows = false;
        boardsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        boardsGrid.Columns.AddRange(new DataGridViewColumn[] {
            vendorColumn, nameColumn, latestVersionColumn, releaseDateColumn, newColumn, lastCheckedColumn, statusColumn });
        boardsGrid.ContextMenuStrip = boardsContextMenu;
        boardsGrid.Dock = DockStyle.Fill;
        boardsGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
        boardsGrid.Location = new Point(0, 25);
        boardsGrid.MultiSelect = false;
        boardsGrid.Name = "boardsGrid";
        boardsGrid.ReadOnly = true;
        boardsGrid.RowHeadersVisible = false;
        boardsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        boardsGrid.Size = new Size(900, 475);
        boardsGrid.TabIndex = 1;
        //
        // vendorColumn
        //
        vendorColumn.HeaderText = "Vendor";
        vendorColumn.Name = "vendorColumn";
        vendorColumn.ReadOnly = true;
        vendorColumn.Width = 70;
        //
        // nameColumn
        //
        nameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        nameColumn.HeaderText = "Name";
        nameColumn.Name = "nameColumn";
        nameColumn.ReadOnly = true;
        //
        // latestVersionColumn
        //
        latestVersionColumn.HeaderText = "Latest Version";
        latestVersionColumn.Name = "latestVersionColumn";
        latestVersionColumn.ReadOnly = true;
        latestVersionColumn.Width = 110;
        //
        // releaseDateColumn
        //
        releaseDateColumn.HeaderText = "Release Date";
        releaseDateColumn.Name = "releaseDateColumn";
        releaseDateColumn.ReadOnly = true;
        releaseDateColumn.Width = 100;
        //
        // newColumn
        //
        newColumn.HeaderText = "NEW";
        newColumn.Name = "newColumn";
        newColumn.ReadOnly = true;
        newColumn.Width = 50;
        //
        // lastCheckedColumn
        //
        lastCheckedColumn.HeaderText = "Last Checked";
        lastCheckedColumn.Name = "lastCheckedColumn";
        lastCheckedColumn.ReadOnly = true;
        lastCheckedColumn.Width = 130;
        //
        // statusColumn
        //
        statusColumn.HeaderText = "Status";
        statusColumn.Name = "statusColumn";
        statusColumn.ReadOnly = true;
        statusColumn.Width = 80;
        //
        // boardsContextMenu
        //
        boardsContextMenu.Items.AddRange(new ToolStripItem[] {
            contextCheckMenuItem, contextEditMenuItem, contextDeleteMenuItem, contextOpenMenuItem });
        boardsContextMenu.Name = "boardsContextMenu";
        boardsContextMenu.Size = new Size(180, 92);
        //
        // contextCheckMenuItem
        //
        contextCheckMenuItem.Name = "contextCheckMenuItem";
        contextCheckMenuItem.Text = "Check this board";
        //
        // contextEditMenuItem
        //
        contextEditMenuItem.Name = "contextEditMenuItem";
        contextEditMenuItem.Text = "Edit…";
        //
        // contextDeleteMenuItem
        //
        contextDeleteMenuItem.Name = "contextDeleteMenuItem";
        contextDeleteMenuItem.Text = "Delete";
        //
        // contextOpenMenuItem
        //
        contextOpenMenuItem.Name = "contextOpenMenuItem";
        contextOpenMenuItem.Text = "Open support page";
        //
        // trayIcon
        //
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayContextMenu;
        trayIcon.Text = "BiosWatcher";
        trayIcon.Visible = true;
        //
        // trayContextMenu
        //
        trayContextMenu.Items.AddRange(new ToolStripItem[] {
            trayOpenMenuItem, trayCheckAllMenuItem, trayContextSeparator, trayExitMenuItem });
        trayContextMenu.Name = "trayContextMenu";
        trayContextMenu.Size = new Size(150, 76);
        //
        // trayOpenMenuItem
        //
        trayOpenMenuItem.Name = "trayOpenMenuItem";
        trayOpenMenuItem.Text = "Open";
        trayOpenMenuItem.Font = new Font(trayOpenMenuItem.Font, FontStyle.Bold);
        //
        // trayCheckAllMenuItem
        //
        trayCheckAllMenuItem.Name = "trayCheckAllMenuItem";
        trayCheckAllMenuItem.Text = "Check all now";
        //
        // trayExitMenuItem
        //
        trayExitMenuItem.Name = "trayExitMenuItem";
        trayExitMenuItem.Text = "Exit";
        //
        // periodicCheckTimer
        //
        periodicCheckTimer.Enabled = false;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 500);
        Controls.Add(emptyStateLabel);
        Controls.Add(boardsGrid);
        Controls.Add(toolStrip);
        MinimumSize = new Size(700, 350);
        Name = "MainForm";
        Text = "BiosWatcher";
        toolStrip.ResumeLayout(false);
        toolStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)boardsGrid).EndInit();
        boardsContextMenu.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
