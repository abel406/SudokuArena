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
    }
}
