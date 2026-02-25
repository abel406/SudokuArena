using System.IO;
using System.Text.Json;

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
            if (!File.Exists(_settingsFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ThemeSettingsDto>(json);
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

    public void SaveThemeMode(ThemeMode mode)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(
                new ThemeSettingsDto(mode.ToString()),
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // No-op: fallo de escritura no debe bloquear la UI.
        }
    }

    private sealed record ThemeSettingsDto(string ThemeMode);
}
