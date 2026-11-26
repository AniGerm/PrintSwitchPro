using System.Text.Json;
using System.Text.Json.Serialization;
using PrintSwitchPro.Core.Models;

namespace PrintSwitchPro.Core.Storage;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _settingsPath;
    private readonly string _legacyBinaryPath;

    public SettingsStore(string? appDataRoot = null)
    {
        var root = appDataRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PrintSwitchPro");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "settings.json");
        _legacyBinaryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "APS",
            "processPrinterMap.dat");
        AppDataDirectory = root;
    }

    public string AppDataDirectory { get; }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                {
                    Normalize(loaded);
                    return loaded;
                }
            }
        }
        catch
        {
            // Fall through to defaults / legacy.
        }

        var settings = new AppSettings();
        TryImportLegacyMappings(settings);
        Normalize(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);
        Directory.CreateDirectory(AppDataDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var temp = _settingsPath + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _settingsPath, overwrite: true);
    }

    private void TryImportLegacyMappings(AppSettings settings)
    {
        // Legacy BinaryFormatter blobs are intentionally not deserialized (security).
        // Keep a marker so the UI can hint that old APS data may need re-mapping.
        if (File.Exists(_legacyBinaryPath))
        {
            settings.DefaultPrinter = settings.DefaultPrinter;
        }
    }

    private static void Normalize(AppSettings settings)
    {
        settings.Language = settings.Language is "en" or "de" ? settings.Language : AppSettings.DefaultLanguage;
        settings.PollIntervalMs = Math.Clamp(settings.PollIntervalMs, 500, 10_000);
        settings.Mappings ??= [];
        settings.Mappings = settings.Mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.ProcessName) && !string.IsNullOrWhiteSpace(m.MappedPrinter))
            .GroupBy(m => m.ProcessName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .Select(m => new ProcessMapping
            {
                ProcessName = m.ProcessName.Trim(),
                MappedPrinter = m.MappedPrinter.Trim(),
            })
            .OrderBy(m => m.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        settings.DefaultPrinter = settings.DefaultPrinter?.Trim() ?? string.Empty;
    }
}
