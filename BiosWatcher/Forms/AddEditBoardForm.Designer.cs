namespace BiosWatcher.Forms;

partial class AddEditBoardForm
{
    private System.ComponentModel.IContainer components = null;

    private Label urlLabel;
    private TextBox urlTextBox;
    private Label displayNameLabel;
    private TextBox displayNameTextBox;
    private Label statusLabel;
    private Button okButton;
    private Button cancelButton;

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
        urlLabel = new Label();
        urlTextBox = new TextBox();
        displayNameLabel = new Label();
        displayNameTextBox = new TextBox();
        statusLabel = new Label();
        okButton = new Button();
        cancelButton = new Button();
        SuspendLayout();
        //
        // urlLabel
        //
        urlLabel.AutoSize = true;
        urlLabel.Location = new Point(12, 15);
        urlLabel.Name = "urlLabel";
        urlLabel.Size = new Size(120, 15);
        urlLabel.Text = "Support/product URL:";
        //
        // urlTextBox
        //
        urlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        urlTextBox.Location = new Point(12, 33);
        urlTextBox.Name = "urlTextBox";
        urlTextBox.Size = new Size(440, 23);
        urlTextBox.TabIndex = 0;
        //
        // displayNameLabel
        //
        displayNameLabel.AutoSize = true;
        displayNameLabel.Location = new Point(12, 65);
        displayNameLabel.Name = "displayNameLabel";
        displayNameLabel.Size = new Size(80, 15);
        displayNameLabel.Text = "Display name:";
        //
        // displayNameTextBox
        //
        displayNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        displayNameTextBox.Location = new Point(12, 83);
        displayNameTextBox.Name = "displayNameTextBox";
        displayNameTextBox.Size = new Size(440, 23);
        displayNameTextBox.TabIndex = 1;
        //
        // statusLabel
        //
        statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        statusLabel.Location = new Point(12, 115);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(440, 45);
        statusLabel.Text = "";
        //
        // okButton
        //
        okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        okButton.Location = new Point(296, 168);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 25);
        okButton.TabIndex = 2;
        okButton.Text = "Add";
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(377, 168);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Cancel";
        //
        // AddEditBoardForm
        //
        AcceptButton = okButton;
        CancelButton = cancelButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(464, 205);
        Controls.Add(urlLabel);
        Controls.Add(urlTextBox);
        Controls.Add(displayNameLabel);
        Controls.Add(displayNameTextBox);
        Controls.Add(statusLabel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AddEditBoardForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add board";
        ResumeLayout(false);
        PerformLayout();
    }
}
