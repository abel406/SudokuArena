using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Theming;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelAutoCompleteTests
{
    private const string PuzzleWithNineGaps =
        "000000000" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286179";

    private const string PuzzleWithFourGaps =
        "000078912" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286179";

    private const string PuzzleWithTwoGaps =
        "534678910" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286170";

    [Fact]
    public void Constructor_ShouldLoadPersistedAutoCompletePreference()
    {
        var store = new FakePreferenceStore
        {
            LoadedAutoCompleteEnabled = true
        };
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);

        Assert.True(viewModel.AutoCompleteEnabled);
    }

    [Fact]
    public void ChangingAutoCompleteEnabled_ShouldPersistPreference()
    {
        var store = new FakePreferenceStore();
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);

        viewModel.AutoCompleteEnabled = true;
        viewModel.AutoCompleteEnabled = false;

        Assert.Equal([true, false], store.SavedAutoCompleteValues);
    }

    [Fact]
    public void AutoComplete_ShouldEnterPromptedState_WhenRemainingIsBetweenFiveAndNine()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps)
        {
            AutoCompleteEnabled = true
        };

        Assert.Equal(9, viewModel.AutoCompleteRemainingToSolve);
        Assert.True(viewModel.IsAutoCompleteTriggerReady);
        Assert.Equal(AutoCompleteSessionState.Prompted, viewModel.AutoCompleteSessionState);
    }

    [Fact]
    public void AutoComplete_ShouldNotPrompt_WhenRemainingIsOutOfRange()
    {
        var viewModel = new MainViewModel(PuzzleWithFourGaps)
        {
            AutoCompleteEnabled = true
        };

        Assert.Equal(4, viewModel.AutoCompleteRemainingToSolve);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
        Assert.Equal(AutoCompleteSessionState.Idle, viewModel.AutoCompleteSessionState);
    }

    [Fact]
    public void CancelAutoCompleteSession_ShouldSetCancelledState_AndDisableTrigger()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps)
        {
            AutoCompleteEnabled = true
        };

        viewModel.CancelAutoCompleteSessionCommand.Execute(null);

        Assert.Equal(AutoCompleteSessionState.Cancelled, viewModel.AutoCompleteSessionState);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
    }

    [Fact]
    public void SelectCell_ShouldNotAutoFill_WhenSessionWasCancelled()
    {
        var viewModel = new MainViewModel(PuzzleWithTwoGaps)
        {
            AutoCompleteEnabled = true
        };
        viewModel.CancelAutoCompleteSessionCommand.Execute(null);

        viewModel.SelectCell(8);

        Assert.Null(viewModel.Cells[8]);
    }

    [Fact]
    public void SelectCell_ShouldNotAutoFill_WhenSessionIsEnabled()
    {
        var viewModel = new MainViewModel(PuzzleWithTwoGaps)
        {
            AutoCompleteEnabled = true
        };

        viewModel.SelectCell(8);

        Assert.Null(viewModel.Cells[8]);
        Assert.Equal(8, viewModel.SelectedCell);
    }

    [Fact]
    public void SelectCell_ShouldNotAutoFill_WhenAutoCompleteIsDisabled()
    {
        var viewModel = new MainViewModel(PuzzleWithTwoGaps)
        {
            AutoCompleteEnabled = false
        };

        viewModel.SelectCell(8);

        Assert.Null(viewModel.Cells[8]);
        Assert.Equal(8, viewModel.SelectedCell);
    }

    [Fact]
    public void SelectCell_ShouldPrioritizeManualSelection_WhenNumberIsSelected()
    {
        var viewModel = new MainViewModel(PuzzleWithTwoGaps)
        {
            AutoCompleteEnabled = true
        };
        viewModel.SelectNumberCommand.Execute(4);

        viewModel.SelectCell(8);

        Assert.Equal(4, viewModel.Cells[8]);
        Assert.True(viewModel.InvalidCells[8]);
        Assert.Equal(1, viewModel.ErrorCount);
    }

    private sealed class FakeDetector : ISystemThemeDetector
    {
        public ThemeMode DetectPreferredMode() => ThemeMode.Light;
    }

    private sealed class FakePreferenceStore : IThemePreferenceStore
    {
        public bool? LoadedAutoCompleteEnabled { get; set; }

        public List<bool> SavedAutoCompleteValues { get; } = [];

        public ThemeMode? LoadThemeMode() => ThemeMode.System;

        public void SaveThemeMode(ThemeMode mode)
        {
        }

        public DifficultyTier? LoadDifficultyTier() => null;

        public void SaveDifficultyTier(DifficultyTier tier)
        {
        }

        public bool? LoadAutoCompleteEnabled() => LoadedAutoCompleteEnabled;

        public void SaveAutoCompleteEnabled(bool enabled)
        {
            SavedAutoCompleteValues.Add(enabled);
        }
    }
}
