using SudokuArena.Application.AutoComplete;
using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Telemetry;
using SudokuArena.Desktop.Theming;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelAutoCompleteTests
{
    private const string SolvedBoard =
        "534678912" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286179";

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
    public void Constructor_ShouldLoadPersistedAutoCompleteTelemetry()
    {
        var store = new FakePreferenceStore
        {
            LoadedTelemetry = new AutoCompleteTelemetrySnapshot(3, 2, 15)
        };
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);

        Assert.Equal(3, viewModel.AutoCompleteStarts);
        Assert.Equal(2, viewModel.AutoCompleteCancels);
        Assert.Equal(15, viewModel.AutoCompleteFilledCells);
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
    public void AutoComplete_ShouldStartAutomatically_WhenRemainingIsWithinRange()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps)
        {
            AutoCompleteEnabled = true
        };

        Assert.Equal(9, viewModel.AutoCompleteRemainingToSolve);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
        Assert.Equal(AutoCompleteSessionState.Running, viewModel.AutoCompleteSessionState);
    }

    [Fact]
    public void AutoComplete_ShouldUsePolicyCalibration_ForTierAndInterval()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard, new AutoCompletePolicyEvaluator())
        {
            SelectedDifficultyTier = DifficultyTier.Beginner,
            AutoCompleteEnabled = true
        };

        Assert.Equal(6, viewModel.AutoCompleteTriggerMinRemaining);
        Assert.Equal(10, viewModel.AutoCompleteTriggerMaxRemaining);
        Assert.Equal(300, viewModel.AutoCompleteTickIntervalMilliseconds);
        Assert.Equal(AutoCompleteSessionState.Running, viewModel.AutoCompleteSessionState);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
    }

    [Fact]
    public void AutoComplete_ShouldUpdatePolicyCalibration_WhenDifficultyChanges()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard, new AutoCompletePolicyEvaluator())
        {
            AutoCompleteEnabled = true
        };

        viewModel.SelectedDifficultyTier = DifficultyTier.Expert;

        Assert.Equal(4, viewModel.AutoCompleteTriggerMinRemaining);
        Assert.Equal(7, viewModel.AutoCompleteTriggerMaxRemaining);
        Assert.Equal(200, viewModel.AutoCompleteTickIntervalMilliseconds);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
        Assert.Equal(AutoCompleteSessionState.Running, viewModel.AutoCompleteSessionState);
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

    [Fact]
    public void StartAutoCompleteSession_ShouldEnterRunning_AndInitializeProgress()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard)
        {
            AutoCompleteEnabled = true
        };

        Assert.Equal(AutoCompleteSessionState.Running, viewModel.AutoCompleteSessionState);
        Assert.True(viewModel.IsAutoCompleteOverlayVisible);
        Assert.False(viewModel.IsAutoCompleteTriggerReady);
        Assert.Equal(9, viewModel.AutoCompleteQueueTotal);
        Assert.Equal(0, viewModel.AutoCompleteQueueCompleted);
        Assert.Equal(1, viewModel.AutoCompleteStarts);
    }

    [Fact]
    public void ProcessAutoCompleteTick_ShouldFillNextCell_AndUpdateCounters()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard)
        {
            AutoCompleteEnabled = true
        };

        viewModel.ProcessAutoCompleteTick();

        Assert.Equal(5, viewModel.Cells[0]);
        Assert.Equal(1, viewModel.AutoCompleteQueueCompleted);
        Assert.Equal(1, viewModel.AutoCompleteFilledCells);
        Assert.Equal("1/9", viewModel.AutoCompleteProgressText);
    }

    [Fact]
    public void ProcessAutoCompleteTick_ShouldFinishSession_WhenQueueCompletes()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard)
        {
            AutoCompleteEnabled = true
        };

        for (var i = 0; i < 9; i++)
        {
            viewModel.ProcessAutoCompleteTick();
        }

        Assert.Equal(AutoCompleteSessionState.Finished, viewModel.AutoCompleteSessionState);
        Assert.True(viewModel.IsGameFinished);
        Assert.True(viewModel.IsVictory);
        Assert.Equal(9, viewModel.AutoCompleteQueueCompleted);
        Assert.Equal(9, viewModel.AutoCompleteFilledCells);
    }

    [Fact]
    public void AutoCompleteSession_ShouldRecordDiagnosticEvents()
    {
        var sink = new FakeDiagnosticsSink();
        var viewModel = new MainViewModel(
            PuzzleWithNineGaps,
            SolvedBoard,
            new AutoCompletePolicyEvaluator(),
            sink)
        {
            AutoCompleteEnabled = true
        };
        viewModel.CancelAutoCompleteSessionCommand.Execute(null);

        Assert.Equal(2, sink.Events.Count);
        Assert.Equal("start", sink.Events[0].EventType);
        Assert.Equal("cancel", sink.Events[1].EventType);
    }

    [Fact]
    public void CancelAutoCompleteSession_ShouldStopRunningSession_AndCountCancellation()
    {
        var viewModel = new MainViewModel(PuzzleWithNineGaps, SolvedBoard)
        {
            AutoCompleteEnabled = true
        };
        viewModel.ProcessAutoCompleteTick();

        viewModel.CancelAutoCompleteSessionCommand.Execute(null);
        viewModel.ProcessAutoCompleteTick();

        Assert.Equal(AutoCompleteSessionState.Cancelled, viewModel.AutoCompleteSessionState);
        Assert.Equal(1, viewModel.AutoCompleteCancels);
        Assert.Equal(0, viewModel.AutoCompleteQueueTotal);
        Assert.False(viewModel.IsAutoCompleteOverlayVisible);
    }

    [Fact]
    public void ChangingTelemetryCounters_ShouldPersistTelemetrySnapshot()
    {
        var store = new FakePreferenceStore();
        var viewModel = new MainViewModel(new ThemeManager(new FakeDetector()), store);
        viewModel.AutoCompleteStarts = 4;
        viewModel.AutoCompleteCancels = 1;
        viewModel.AutoCompleteFilledCells = 12;

        Assert.NotNull(store.LastTelemetry);
        Assert.Equal(viewModel.AutoCompleteStarts, store.LastTelemetry!.Starts);
        Assert.Equal(viewModel.AutoCompleteCancels, store.LastTelemetry.Cancellations);
        Assert.Equal(viewModel.AutoCompleteFilledCells, store.LastTelemetry.FilledCells);
    }

    private sealed class FakeDetector : ISystemThemeDetector
    {
        public ThemeMode DetectPreferredMode() => ThemeMode.Light;
    }

    private sealed class FakePreferenceStore : IThemePreferenceStore
    {
        public bool? LoadedAutoCompleteEnabled { get; set; }

        public List<bool> SavedAutoCompleteValues { get; } = [];

        public AutoCompleteTelemetrySnapshot? LoadedTelemetry { get; set; }

        public AutoCompleteTelemetrySnapshot? LastTelemetry { get; private set; }

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

        public AutoCompleteTelemetrySnapshot? LoadAutoCompleteTelemetry() => LoadedTelemetry;

        public void SaveAutoCompleteTelemetry(AutoCompleteTelemetrySnapshot snapshot)
        {
            LastTelemetry = snapshot;
        }
    }

    private sealed class FakeDiagnosticsSink : IAutoCompleteDiagnosticsSink
    {
        public List<AutoCompleteDiagnosticEvent> Events { get; } = [];

        public void Record(AutoCompleteDiagnosticEvent diagnosticEvent)
        {
            Events.Add(diagnosticEvent);
        }
    }
}
