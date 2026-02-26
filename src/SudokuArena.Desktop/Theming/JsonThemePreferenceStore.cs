using System.IO;
using System.Text.Json;
using SudokuArena.Application.Puzzles;

namespace SudokuArena.Desktop.Theming;

public sealed class JsonThemePreferenceStore : IThemePreferenceStore
{
    private readonly string _settingsFilePath;

    public JsonThemePreferenceStore(string? settingsFilePath = null)
    {
        _settingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SudokuArena",
                "desktop-settings.json")
            : settingsFilePath;
    }

    public ThemeMode? LoadThemeMode()
    {
        try
        {
            var settings = LoadSettings();
            if (string.IsNullOrWhiteSpace(settings?.ThemeMode))
            {
                return null;
            }

            return Enum.TryParse<ThemeMode>(settings.ThemeMode, ignoreCase: true, out var mode)
                ? mode
                : null;
        }
        catch
        {
            return null;
        }
    }

    public DifficultyTier? LoadDifficultyTier()
    {
        try
        {
            var settings = LoadSettings();
            if (string.IsNullOrWhiteSpace(settings?.DifficultyTier))
            {
                return null;
            }

            return Enum.TryParse<DifficultyTier>(settings.DifficultyTier, ignoreCase: true, out var tier)
                ? tier
                : null;
        }
        catch
        {
            return null;
        }
    }

    public void SaveThemeMode(ThemeMode mode)
    {
        try
        {
            var settings = LoadSettings() ?? new ThemeSettingsDto();
            settings.ThemeMode = mode.ToString();
            SaveSettings(settings);
        }
        catch
        {
            // No-op: fallo de escritura no debe bloquear la UI.
        }
    }

    public void SaveDifficultyTier(DifficultyTier tier)
    {
        try
        {
            var settings = LoadSettings() ?? new ThemeSettingsDto();
            settings.DifficultyTier = tier.ToString();
            SaveSettings(settings);
        }
        catch
        {
            // No-op: fallo de escritura no debe bloquear la UI.
        }
    }

    public bool? LoadAutoCompleteEnabled()
    {
        try
        {
            var settings = LoadSettings();
            return settings?.AutoCompleteEnabled;
        }
        catch
        {
            return null;
        }
    }

    public void SaveAutoCompleteEnabled(bool enabled)
    {
        try
        {
            var settings = LoadSettings() ?? new ThemeSettingsDto();
            settings.AutoCompleteEnabled = enabled;
            SaveSettings(settings);
        }
        catch
        {
            // No-op: fallo de escritura no debe bloquear la UI.
        }
    }

    public AutoCompleteTelemetrySnapshot? LoadAutoCompleteTelemetry()
    {
        try
        {
            var settings = LoadSettings();
            if (settings is null)
            {
                return null;
            }

            var starts = Math.Max(0, settings.AutoCompleteStarts ?? 0);
            var cancellations = Math.Max(0, settings.AutoCompleteCancellations ?? 0);
            var filledCells = Math.Max(0, settings.AutoCompleteFilledCells ?? 0);
            return new AutoCompleteTelemetrySnapshot(starts, cancellations, filledCells);
        }
        catch
        {
            return null;
        }
    }

    public void SaveAutoCompleteTelemetry(AutoCompleteTelemetrySnapshot snapshot)
    {
        try
        {
            var settings = LoadSettings() ?? new ThemeSettingsDto();
            settings.AutoCompleteStarts = Math.Max(0, snapshot.Starts);
            settings.AutoCompleteCancellations = Math.Max(0, snapshot.Cancellations);
            settings.AutoCompleteFilledCells = Math.Max(0, snapshot.FilledCells);
            SaveSettings(settings);
        }
        catch
        {
            // No-op: fallo de escritura no debe bloquear la UI.
        }
    }

    private ThemeSettingsDto? LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(_settingsFilePath);
        return JsonSerializer.Deserialize<ThemeSettingsDto>(json);
    }

    private void SaveSettings(ThemeSettingsDto settings)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_settingsFilePath, json);
    }

    private sealed class ThemeSettingsDto
    {
        public string? ThemeMode { get; set; }

        public string? DifficultyTier { get; set; }

        public bool? AutoCompleteEnabled { get; set; }

        public int? AutoCompleteStarts { get; set; }

        public int? AutoCompleteCancellations { get; set; }

        public int? AutoCompleteFilledCells { get; set; }
    }
}
