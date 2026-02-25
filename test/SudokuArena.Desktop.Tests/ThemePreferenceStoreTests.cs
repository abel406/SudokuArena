using SudokuArena.Desktop.Theming;
using SudokuArena.Application.Puzzles;

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
    public void SaveAndLoadDifficultyTier_ShouldRoundTrip()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        store.SaveDifficultyTier(DifficultyTier.Expert);
        var loaded = store.LoadDifficultyTier();

        Assert.Equal(DifficultyTier.Expert, loaded);
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

    [Fact]
    public void LoadDifficultyTier_ShouldReturnNull_WhenStoredValueIsInvalid()
    {
        var path = BuildTempFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{ \"DifficultyTier\": \"NotATier\" }");
        var store = new JsonThemePreferenceStore(path);

        var loaded = store.LoadDifficultyTier();

        Assert.Null(loaded);
    }

    [Fact]
    public void SaveThemeAndDifficulty_ShouldPreserveBothValues()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        store.SaveThemeMode(ThemeMode.Dark);
        store.SaveDifficultyTier(DifficultyTier.Hard);

        Assert.Equal(ThemeMode.Dark, store.LoadThemeMode());
        Assert.Equal(DifficultyTier.Hard, store.LoadDifficultyTier());
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
