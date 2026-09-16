namespace BiosWatcher.Forms;

partial class BoardDetailForm
{
    private System.ComponentModel.IContainer components = null;

    private Label titleLabel;
    private Label statusLabel;
    private ListBox releasesListBox;
    private Label changelogLabel;
    private TextBox changelogTextBox;
    private Label sizeLabel;
    private Label sizeValueLabel;
    private Label shaLabel;
    private TextBox shaTextBox;
    private Button downloadButton;
    private Button openSupportPageButton;
    private Button closeButton;

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
        titleLabel = new Label();
        statusLabel = new Label();
        releasesListBox = new ListBox();
        changelogLabel = new Label();
        changelogTextBox = new TextBox();
        sizeLabel = new Label();
        sizeValueLabel = new Label();
        shaLabel = new Label();
        shaTextBox = new TextBox();
        downloadButton = new Button();
        openSupportPageButton = new Button();
        closeButton = new Button();
        SuspendLayout();
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        titleLabel.Location = new Point(12, 9);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(100, 20);
        titleLabel.Text = "Board title";
        //
        // statusLabel
        //
        statusLabel.AutoSize = false;
        statusLabel.Location = new Point(12, 34);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(600, 20);
        statusLabel.Text = "";
        //
        // releasesListBox
        //
        releasesListBox.FormattingEnabled = true;
        releasesListBox.IntegralHeight = false;
        releasesListBox.Location = new Point(12, 58);
        releasesListBox.Name = "releasesListBox";
        releasesListBox.Size = new Size(190, 300);
        releasesListBox.TabIndex = 0;
        //
        // changelogLabel
        //
        changelogLabel.AutoSize = true;
        changelogLabel.Location = new Point(215, 58);
        changelogLabel.Name = "changelogLabel";
        changelogLabel.Size = new Size(70, 15);
        changelogLabel.Text = "Changelog:";
        //
        // changelogTextBox
        //
        changelogTextBox.Location = new Point(215, 76);
        changelogTextBox.Multiline = true;
        changelogTextBox.Name = "changelogTextBox";
        changelogTextBox.ReadOnly = true;
        changelogTextBox.ScrollBars = ScrollBars.Vertical;
        changelogTextBox.Size = new Size(413, 170);
        changelogTextBox.TabIndex = 1;
        //
        // sizeLabel
        //
        sizeLabel.AutoSize = true;
        sizeLabel.Location = new Point(215, 256);
        sizeLabel.Name = "sizeLabel";
        sizeLabel.Size = new Size(32, 15);
        sizeLabel.Text = "Size:";
        //
        // sizeValueLabel
        //
        sizeValueLabel.AutoSize = true;
        sizeValueLabel.Location = new Point(270, 256);
        sizeValueLabel.Name = "sizeValueLabel";
        sizeValueLabel.Size = new Size(70, 15);
        sizeValueLabel.Text = "";
        //
        // shaLabel
        //
        shaLabel.AutoSize = true;
        shaLabel.Location = new Point(215, 279);
        shaLabel.Name = "shaLabel";
        shaLabel.Size = new Size(60, 15);
        shaLabel.Text = "SHA-256:";
        //
        // shaTextBox
        //
        shaTextBox.Location = new Point(215, 296);
        shaTextBox.Name = "shaTextBox";
        shaTextBox.ReadOnly = true;
        shaTextBox.Size = new Size(413, 23);
        shaTextBox.TabIndex = 2;
        //
        // downloadButton
        //
        downloadButton.Location = new Point(215, 336);
        downloadButton.Name = "downloadButton";
        downloadButton.Size = new Size(120, 28);
        downloadButton.TabIndex = 3;
        downloadButton.Text = "Download BIOS";
        //
        // openSupportPageButton
        //
        openSupportPageButton.Location = new Point(345, 336);
        openSupportPageButton.Name = "openSupportPageButton";
        openSupportPageButton.Size = new Size(150, 28);
        openSupportPageButton.TabIndex = 4;
        openSupportPageButton.Text = "Open support page";
        //
        // closeButton
        //
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Location = new Point(553, 336);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(75, 28);
        closeButton.TabIndex = 5;
        closeButton.Text = "Close";
        //
        // BoardDetailForm
        //
        AcceptButton = null;
        CancelButton = closeButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(640, 420);
        Controls.Add(titleLabel);
        Controls.Add(statusLabel);
        Controls.Add(releasesListBox);
        Controls.Add(changelogLabel);
        Controls.Add(changelogTextBox);
        Controls.Add(sizeLabel);
        Controls.Add(sizeValueLabel);
        Controls.Add(shaLabel);
        Controls.Add(shaTextBox);
        Controls.Add(downloadButton);
        Controls.Add(openSupportPageButton);
        Controls.Add(closeButton);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "BoardDetailForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "BIOS details";
        ResumeLayout(false);
        PerformLayout();
    }
}
