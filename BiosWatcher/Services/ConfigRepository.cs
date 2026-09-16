using System.Text.Json;
using BiosWatcher.Models;

namespace BiosWatcher.Services;

/// <summary>
/// Loads and saves the single-file JSON config at %APPDATA%\BiosWatcher\config.json.
/// Writes are atomic (.tmp then File.Replace) so a crash mid-write cannot corrupt the file.
/// </summary>
public class ConfigRepository(string? configPath = null)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new DateOnlyJsonConverter() },
    };

    private readonly string _configPath = configPath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BiosWatcher", "config.json");

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Never overwrite an unreadable config with an empty model: preserve it for inspection.
            BackupCorruptFile();
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(dir);

        var tmpPath = _configPath + ".tmp";
        File.WriteAllText(tmpPath, JsonSerializer.Serialize(config, JsonOptions));

        if (File.Exists(_configPath))
            File.Replace(tmpPath, _configPath, destinationBackupFileName: null);
        else
            File.Move(tmpPath, _configPath);
    }

    private void BackupCorruptFile()
    {
        try
        {
            File.Copy(_configPath, _configPath + ".bak", overwrite: true);
        }
        catch (IOException)
        {
            // Best-effort backup; do not throw out of a failure-recovery path.
        }
    }
}
