using SudokuArena.Desktop.Theming;

namespace SudokuArena.Desktop.Tests;

public sealed class ThemePreferenceStoreTests
{
    [Fact]
    public void LoadThemeMode_ShouldReturnNull_WhenSettingsFileDoesNotExist()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        var loaded = store.LoadThemeMode();

        Assert.Null(loaded);
    }

    [Fact]
    public void SaveAndLoadThemeMode_ShouldRoundTrip()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        store.SaveThemeMode(ThemeMode.Dark);
        var loaded = store.LoadThemeMode();

        Assert.Equal(ThemeMode.Dark, loaded);
    }

    [Fact]
    public void LoadThemeMode_ShouldReturnNull_WhenStoredValueIsInvalid()
    {
        var path = BuildTempFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ \"ThemeMode\": \"NotARealMode\" }");
        var store = new JsonThemePreferenceStore(path);

        var loaded = store.LoadThemeMode();

        Assert.Null(loaded);
    }

    private static string BuildTempFilePath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "SudokuArenaTests",
            Guid.NewGuid().ToString("N"),
            "desktop-settings.json");
    }
}
