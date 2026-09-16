using BiosWatcher.Models;

namespace BiosWatcher.Forms;

public partial class SettingsForm : Form
{
    private readonly AppSettings _settings;

    /// <summary>True when Save was clicked with a changed CheckIntervalHours, so the caller knows to restart the periodic timer.</summary>
    public bool IntervalChanged { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        InitializeComponent();

        intervalUpDown.Value = Math.Clamp(settings.CheckIntervalHours, (int)intervalUpDown.Minimum, (int)intervalUpDown.Maximum);
        ignoreBetaCheckBox.Checked = settings.IgnoreBetaVersions;
        newFlagDaysUpDown.Value = Math.Clamp(settings.NewFlagDays, (int)newFlagDaysUpDown.Minimum, (int)newFlagDaysUpDown.Maximum);

        okButton.Click += (_, _) => Save();
    }

    private void Save()
    {
        var newInterval = (int)intervalUpDown.Value;
        IntervalChanged = newInterval != _settings.CheckIntervalHours;

        _settings.CheckIntervalHours = newInterval;
        _settings.IgnoreBetaVersions = ignoreBetaCheckBox.Checked;
        _settings.NewFlagDays = (int)newFlagDaysUpDown.Value;

        DialogResult = DialogResult.OK;
        Close();
    }
}
