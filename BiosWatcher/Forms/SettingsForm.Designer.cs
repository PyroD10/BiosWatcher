namespace BiosWatcher.Forms;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    private Label intervalLabel;
    private NumericUpDown intervalUpDown;
    private Label intervalUnitLabel;
    private CheckBox ignoreBetaCheckBox;
    private Label newFlagDaysLabel;
    private NumericUpDown newFlagDaysUpDown;
    private Label newFlagDaysUnitLabel;
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
        intervalLabel = new Label();
        intervalUpDown = new NumericUpDown();
        intervalUnitLabel = new Label();
        ignoreBetaCheckBox = new CheckBox();
        newFlagDaysLabel = new Label();
        newFlagDaysUpDown = new NumericUpDown();
        newFlagDaysUnitLabel = new Label();
        okButton = new Button();
        cancelButton = new Button();
        ((System.ComponentModel.ISupportInitialize)intervalUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)newFlagDaysUpDown).BeginInit();
        SuspendLayout();
        //
        // intervalLabel
        //
        intervalLabel.AutoSize = true;
        intervalLabel.Location = new Point(12, 17);
        intervalLabel.Name = "intervalLabel";
        intervalLabel.Size = new Size(120, 15);
        intervalLabel.Text = "Check every:";
        //
        // intervalUpDown
        //
        intervalUpDown.Location = new Point(150, 14);
        intervalUpDown.Maximum = 168;
        intervalUpDown.Minimum = 1;
        intervalUpDown.Name = "intervalUpDown";
        intervalUpDown.Size = new Size(60, 23);
        intervalUpDown.TabIndex = 0;
        intervalUpDown.Value = 24;
        //
        // intervalUnitLabel
        //
        intervalUnitLabel.AutoSize = true;
        intervalUnitLabel.Location = new Point(216, 17);
        intervalUnitLabel.Name = "intervalUnitLabel";
        intervalUnitLabel.Size = new Size(40, 15);
        intervalUnitLabel.Text = "hours";
        //
        // ignoreBetaCheckBox
        //
        ignoreBetaCheckBox.AutoSize = true;
        ignoreBetaCheckBox.Location = new Point(12, 50);
        ignoreBetaCheckBox.Name = "ignoreBetaCheckBox";
        ignoreBetaCheckBox.Size = new Size(230, 19);
        ignoreBetaCheckBox.TabIndex = 1;
        ignoreBetaCheckBox.Text = "Ignore beta versions (where marked)";
        //
        // newFlagDaysLabel
        //
        newFlagDaysLabel.AutoSize = true;
        newFlagDaysLabel.Location = new Point(12, 84);
        newFlagDaysLabel.Name = "newFlagDaysLabel";
        newFlagDaysLabel.Size = new Size(120, 15);
        newFlagDaysLabel.Text = "Flag \"NEW\" for:";
        //
        // newFlagDaysUpDown
        //
        newFlagDaysUpDown.Location = new Point(150, 81);
        newFlagDaysUpDown.Maximum = 90;
        newFlagDaysUpDown.Minimum = 1;
        newFlagDaysUpDown.Name = "newFlagDaysUpDown";
        newFlagDaysUpDown.Size = new Size(60, 23);
        newFlagDaysUpDown.TabIndex = 2;
        newFlagDaysUpDown.Value = 14;
        //
        // newFlagDaysUnitLabel
        //
        newFlagDaysUnitLabel.AutoSize = true;
        newFlagDaysUnitLabel.Location = new Point(216, 84);
        newFlagDaysUnitLabel.Name = "newFlagDaysUnitLabel";
        newFlagDaysUnitLabel.Size = new Size(30, 15);
        newFlagDaysUnitLabel.Text = "days";
        //
        // okButton
        //
        okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        okButton.Location = new Point(157, 128);
        okButton.Name = "okButton";
        okButton.Size = new Size(75, 25);
        okButton.TabIndex = 3;
        okButton.Text = "Save";
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(238, 128);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(75, 25);
        cancelButton.TabIndex = 4;
        cancelButton.Text = "Cancel";
        //
        // SettingsForm
        //
        AcceptButton = okButton;
        CancelButton = cancelButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(325, 165);
        Controls.Add(intervalLabel);
        Controls.Add(intervalUpDown);
        Controls.Add(intervalUnitLabel);
        Controls.Add(ignoreBetaCheckBox);
        Controls.Add(newFlagDaysLabel);
        Controls.Add(newFlagDaysUpDown);
        Controls.Add(newFlagDaysUnitLabel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        ((System.ComponentModel.ISupportInitialize)intervalUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)newFlagDaysUpDown).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
