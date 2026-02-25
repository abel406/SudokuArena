using Microsoft.Win32;

namespace SudokuArena.Desktop.Theming;

public sealed class WindowsThemeDetector : ISystemThemeDetector
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public ThemeMode DetectPreferredMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        var value = key?.GetValue(AppsUseLightThemeValueName);
        if (value is int intValue)
        {
            return intValue == 0 ? ThemeMode.Dark : ThemeMode.Light;
        }

        if (value is byte byteValue)
        {
            return byteValue == 0 ? ThemeMode.Dark : ThemeMode.Light;
        }

        return ThemeMode.Light;
    }
}
