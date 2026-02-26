using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Theming;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelDifficultyPreferenceTests
{
    [Fact]
    public void Constructor_ShouldLoadPersistedDifficultyTier()
    {
        var store = new FakePreferenceStore
        {
            LoadedDifficultyTier = DifficultyTier.Hard
        };
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);

        Assert.Equal(DifficultyTier.Hard, viewModel.SelectedDifficultyTier);
        Assert.Equal("Dificil", viewModel.DifficultyLabel);
    }

    [Fact]
    public void ChangingSelectedDifficultyTier_ShouldPersistSelection()
    {
        var store = new FakePreferenceStore();
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);

        viewModel.SelectedDifficultyTier = DifficultyTier.Expert;

        Assert.Equal([DifficultyTier.Expert], store.SavedDifficultyTiers);
    }

    private sealed class FakeDetector : ISystemThemeDetector
    {
        public ThemeMode DetectPreferredMode() => ThemeMode.Light;
    }

    private sealed class FakePreferenceStore : IThemePreferenceStore
    {
        public DifficultyTier? LoadedDifficultyTier { get; set; }

        public List<DifficultyTier> SavedDifficultyTiers { get; } = [];

        public ThemeMode? LoadThemeMode() => ThemeMode.System;

        public void SaveThemeMode(ThemeMode mode)
        {
        }

        public DifficultyTier? LoadDifficultyTier() => LoadedDifficultyTier;

        public void SaveDifficultyTier(DifficultyTier tier)
        {
            SavedDifficultyTiers.Add(tier);
        }

        public bool? LoadAutoCompleteEnabled() => null;

        public void SaveAutoCompleteEnabled(bool enabled)
        {
        }

        public AutoCompleteTelemetrySnapshot? LoadAutoCompleteTelemetry() => null;

        public void SaveAutoCompleteTelemetry(AutoCompleteTelemetrySnapshot snapshot)
        {
        }
    }
}
