using System.Windows;

namespace SudokuArena.Desktop.Theming;

public sealed class ThemeManager(ISystemThemeDetector systemThemeDetector)
{
    private static readonly Uri LightThemeDictionaryUri = new(
        "/SudokuArena.Desktop;component/Themes/Theme.Light.xaml",
        UriKind.Relative);

    private static readonly Uri DarkThemeDictionaryUri = new(
        "/SudokuArena.Desktop;component/Themes/Theme.Dark.xaml",
        UriKind.Relative);

    public event Action<ThemeMode>? ThemeApplied;

    public ThemeMode RequestedMode { get; private set; } = ThemeMode.System;

    public ThemeMode EffectiveMode { get; private set; } = ThemeMode.Light;

    public ThemeMode ResolveEffectiveMode(ThemeMode mode)
    {
        return mode switch
        {
            ThemeMode.Light => ThemeMode.Light,
            ThemeMode.Dark => ThemeMode.Dark,
            _ => ResolveSystemMode()
        };
    }

    public ThemeMode ApplyTheme(ThemeMode mode)
    {
        RequestedMode = mode;
        EffectiveMode = ResolveEffectiveMode(mode);
        ApplyDictionary(EffectiveMode);
        ThemeApplied?.Invoke(EffectiveMode);
        return EffectiveMode;
    }

    private ThemeMode ResolveSystemMode()
    {
        try
        {
            var detected = systemThemeDetector.DetectPreferredMode();
            return detected is ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
        }
        catch
        {
            return ThemeMode.Light;
        }
    }

    private static void ApplyDictionary(ThemeMode effectiveMode)
    {
        var resources = System.Windows.Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var dictionaries = resources.MergedDictionaries;
        for (var i = dictionaries.Count - 1; i >= 0; i--)
        {
            var source = dictionaries[i].Source?.ToString();
            if (source is null)
            {
                continue;
            }

            if (source.Contains("/Themes/Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("/Themes/Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase))
            {
                dictionaries.RemoveAt(i);
            }
        }

        dictionaries.Add(new ResourceDictionary
        {
            Source = effectiveMode == ThemeMode.Dark ? DarkThemeDictionaryUri : LightThemeDictionaryUri
        });
    }
}
