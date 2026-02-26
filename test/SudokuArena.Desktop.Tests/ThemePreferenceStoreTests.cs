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
    public void SaveAndLoadAutoCompleteEnabled_ShouldRoundTrip()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        store.SaveAutoCompleteEnabled(true);
        var loaded = store.LoadAutoCompleteEnabled();

        Assert.True(loaded);
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

    [Fact]
    public void SaveThemeDifficultyAndAutoComplete_ShouldPreserveAllValues()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);

        store.SaveThemeMode(ThemeMode.Light);
        store.SaveDifficultyTier(DifficultyTier.Medium);
        store.SaveAutoCompleteEnabled(false);

        Assert.Equal(ThemeMode.Light, store.LoadThemeMode());
        Assert.Equal(DifficultyTier.Medium, store.LoadDifficultyTier());
        Assert.False(store.LoadAutoCompleteEnabled());
    }

    [Fact]
    public void SaveAndLoadAutoCompleteTelemetry_ShouldRoundTrip()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);
        var snapshot = new AutoCompleteTelemetrySnapshot(5, 2, 17);

        store.SaveAutoCompleteTelemetry(snapshot);
        var loaded = store.LoadAutoCompleteTelemetry();

        Assert.NotNull(loaded);
        Assert.Equal(snapshot, loaded);
    }

    [Fact]
    public void SaveAllPreferencesAndTelemetry_ShouldPreserveValues()
    {
        var path = BuildTempFilePath();
        var store = new JsonThemePreferenceStore(path);
        var telemetry = new AutoCompleteTelemetrySnapshot(9, 3, 28);

        store.SaveThemeMode(ThemeMode.Dark);
        store.SaveDifficultyTier(DifficultyTier.Beginner);
        store.SaveAutoCompleteEnabled(true);
        store.SaveAutoCompleteTelemetry(telemetry);

        Assert.Equal(ThemeMode.Dark, store.LoadThemeMode());
        Assert.Equal(DifficultyTier.Beginner, store.LoadDifficultyTier());
        Assert.True(store.LoadAutoCompleteEnabled());
        Assert.Equal(telemetry, store.LoadAutoCompleteTelemetry());
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
