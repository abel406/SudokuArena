using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SudokuArena.Application.AutoComplete;
using SudokuArena.Application.Puzzles;
using SudokuArena.Application.Scoring;
using SudokuArena.Desktop.Telemetry;
using SudokuArena.Desktop.Theming;
using SudokuArena.Domain.Models;

namespace SudokuArena.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int ErrorLimitValue = 3;
    private const int DefaultAutoCompleteTickIntervalMilliseconds = 250;

    private readonly int?[] _cells = new int?[81];
    private readonly bool[] _givens = new bool[81];
    private readonly bool[] _editableCells = new bool[81];
    private readonly bool[] _invalidCells = new bool[81];
    private readonly Stack<MoveEntry> _moveHistory = new();
    private IReadOnlyList<int?> _cellsView = Array.Empty<int?>();
    private IReadOnlyList<bool> _givenCellsView = Array.Empty<bool>();
    private IReadOnlyList<bool> _editableCellsView = Array.Empty<bool>();
    private IReadOnlyList<bool> _invalidCellsView = Array.Empty<bool>();
    private DateTimeOffset _gameStartedUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset? _clockPausedAtUtc;
    private TimeSpan _pausedClockDuration = TimeSpan.Zero;
    private bool _isDefeatDialogOpen;
    private readonly Queue<AutoCompleteStep> _autoCompleteQueue = new();
    private int[] _resolvedSolution = [];
    private IReadOnlyList<int> _currentPuzzleTimeThresholds = [150, 210, 300, 420];
    private DateTimeOffset _lastCorrectFillAtUtc = DateTimeOffset.UtcNow;
    private readonly SudokuScoreEngine _scoreEngine = new();
    private readonly ScoreAccumulator _scoreAccumulator = new();
    private const ScoreVersion ActiveScoreVersion = ScoreVersion.Old;
    private readonly ThemeManager? _themeManager;
    private readonly IThemePreferenceStore? _themePreferenceStore;
    private readonly IPuzzleProvider? _puzzleProvider;
    private readonly IAutoCompletePolicyEvaluator? _autoCompletePolicyEvaluator;
    private readonly IAutoCompleteDiagnosticsSink? _autoCompleteDiagnosticsSink;

    public MainViewModel()
        : this(SudokuDefaults.SamplePuzzle, null, null, null, null, null, null)
    {
    }

    public MainViewModel(string puzzle)
        : this(puzzle, null, null, null, null, null, null)
    {
    }

    public MainViewModel(string puzzle, string solution)
        : this(puzzle, solution, null, null, null, null, null)
    {
    }

    public MainViewModel(string puzzle, string solution, IAutoCompletePolicyEvaluator autoCompletePolicyEvaluator)
        : this(puzzle, solution, null, null, null, autoCompletePolicyEvaluator, null)
    {
    }

    public MainViewModel(
        string puzzle,
        string solution,
        IAutoCompletePolicyEvaluator autoCompletePolicyEvaluator,
        IAutoCompleteDiagnosticsSink autoCompleteDiagnosticsSink)
        : this(puzzle, solution, null, null, null, autoCompletePolicyEvaluator, autoCompleteDiagnosticsSink)
    {
    }

    public MainViewModel(ThemeManager themeManager)
        : this(SudokuDefaults.SamplePuzzle, null, themeManager, null, null, null, null)
    {
    }

    public MainViewModel(ThemeManager themeManager, IThemePreferenceStore themePreferenceStore)
        : this(SudokuDefaults.SamplePuzzle, null, themeManager, themePreferenceStore, null, null, null)
    {
    }

    public MainViewModel(IPuzzleProvider puzzleProvider)
        : this(SudokuDefaults.SamplePuzzle, null, null, null, puzzleProvider, null, null)
    {
    }

    public MainViewModel(
        ThemeManager themeManager,
        IThemePreferenceStore themePreferenceStore,
        IPuzzleProvider puzzleProvider)
        : this(SudokuDefaults.SamplePuzzle, null, themeManager, themePreferenceStore, puzzleProvider, null, null)
    {
    }

    public MainViewModel(
        ThemeManager themeManager,
        IThemePreferenceStore themePreferenceStore,
        IPuzzleProvider puzzleProvider,
        IAutoCompletePolicyEvaluator autoCompletePolicyEvaluator)
        : this(
            SudokuDefaults.SamplePuzzle,
            null,
            themeManager,
            themePreferenceStore,
            puzzleProvider,
            autoCompletePolicyEvaluator,
            null)
    {
    }

    public MainViewModel(
        ThemeManager themeManager,
        IThemePreferenceStore themePreferenceStore,
        IPuzzleProvider puzzleProvider,
        IAutoCompletePolicyEvaluator autoCompletePolicyEvaluator,
        IAutoCompleteDiagnosticsSink autoCompleteDiagnosticsSink)
        : this(
            SudokuDefaults.SamplePuzzle,
            null,
            themeManager,
            themePreferenceStore,
            puzzleProvider,
            autoCompletePolicyEvaluator,
            autoCompleteDiagnosticsSink)
    {
    }

    private MainViewModel(
        string puzzle,
        string? solution,
        ThemeManager? themeManager,
        IThemePreferenceStore? themePreferenceStore,
        IPuzzleProvider? puzzleProvider = null,
        IAutoCompletePolicyEvaluator? autoCompletePolicyEvaluator = null,
        IAutoCompleteDiagnosticsSink? autoCompleteDiagnosticsSink = null)
    {
        _themeManager = themeManager;
        _themePreferenceStore = themePreferenceStore;
        _puzzleProvider = puzzleProvider;
        _autoCompletePolicyEvaluator = autoCompletePolicyEvaluator;
        _autoCompleteDiagnosticsSink = autoCompleteDiagnosticsSink;
        NumberOptions = new ObservableCollection<NumberOptionItem>(
            Enumerable.Range(1, 9).Select(x => new NumberOptionItem(x)));
        ThemeModeOptions = Enum.GetValues<ThemeMode>();
        DifficultyTierOptions = Enum.GetValues<DifficultyTier>();
        var initialThemeMode = _themePreferenceStore?.LoadThemeMode() ?? ThemeMode.System;
        _themeMode = initialThemeMode;
        ApplyThemeSelection(initialThemeMode, persistSelection: false);
        var initialDifficultyTier = _themePreferenceStore?.LoadDifficultyTier() ?? DifficultyTier.Medium;
        _selectedDifficultyTier = initialDifficultyTier;
        DifficultyLabel = ToDifficultyLabel(initialDifficultyTier);
        _autoCompleteEnabled = _themePreferenceStore?.LoadAutoCompleteEnabled() ?? false;
        var initialTelemetry = _themePreferenceStore?.LoadAutoCompleteTelemetry();
        if (initialTelemetry is not null)
        {
            _autoCompleteStarts = initialTelemetry.Starts;
            _autoCompleteCancels = initialTelemetry.Cancellations;
            _autoCompleteFilledCells = initialTelemetry.FilledCells;
        }
        LoadPuzzle(puzzle, solution, null);
    }

    [ObservableProperty]
    private string _playerEmail = "player1@gmail.com";

    [ObservableProperty]
    private string _opponentEmail = "player2@gmail.com";

    [ObservableProperty]
    private int _selectedCell = -1;

    [ObservableProperty]
    private int _selectedNumber;

    [ObservableProperty]
    private string _statusMessage = "Listo para duelo LAN.";

    [ObservableProperty]
    private bool _isLanMode = true;

    [ObservableProperty]
    private int _score;

    [ObservableProperty]
    private int _oldScore;

    [ObservableProperty]
    private int _newScore;

    [ObservableProperty]
    private string _difficultyLabel = "Medio";

    [ObservableProperty]
    private DifficultyTier _selectedDifficultyTier = DifficultyTier.Medium;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private string _elapsedTimeText = "00:00";

    [ObservableProperty]
    private bool _autoCompleteEnabled;

    [ObservableProperty]
    private AutoCompleteSessionState _autoCompleteSessionState = AutoCompleteSessionState.Idle;

    [ObservableProperty]
    private bool _isAutoCompleteTriggerReady;

    [ObservableProperty]
    private int _autoCompleteRemainingToSolve;

    [ObservableProperty]
    private int _autoCompleteTriggerMinRemaining = 5;

    [ObservableProperty]
    private int _autoCompleteTriggerMaxRemaining = 9;

    [ObservableProperty]
    private int _autoCompleteTickIntervalMilliseconds = DefaultAutoCompleteTickIntervalMilliseconds;

    [ObservableProperty]
    private int _autoCompleteQueueTotal;

    [ObservableProperty]
    private int _autoCompleteQueueCompleted;

    [ObservableProperty]
    private int _autoCompleteCurrentDigit;

    [ObservableProperty]
    private int _autoCompleteStarts;

    [ObservableProperty]
    private int _autoCompleteCancels;

    [ObservableProperty]
    private int _autoCompleteFilledCells;

    [ObservableProperty]
    private bool _isDeleteMode;

    [ObservableProperty]
    private bool _isGameFinished;

    [ObservableProperty]
    private bool _isVictory;

    [ObservableProperty]
    private VictorySummary? _lastVictorySummary;

    [ObservableProperty]
    private bool _isAwaitingDefeatDecision;

    [ObservableProperty]
    private ThemeMode _themeMode = ThemeMode.System;

    [ObservableProperty]
    private ThemeMode _effectiveThemeMode = ThemeMode.Light;

    public ObservableCollection<NumberOptionItem> NumberOptions { get; }

    public IReadOnlyList<ThemeMode> ThemeModeOptions { get; }

    public IReadOnlyList<DifficultyTier> DifficultyTierOptions { get; }

    public event EventHandler? DefeatThresholdReached;

    public event EventHandler? GameWon;

    public event EventHandler? GameLost;

    public event EventHandler<CompletionUnitsEventArgs>? CompletionUnitsAchieved;

    public IReadOnlyList<int?> Cells => _cellsView;

    public IReadOnlyList<bool> GivenCells => _givenCellsView;

    public IReadOnlyList<bool> EditableCells => _editableCellsView;

    public IReadOnlyList<bool> InvalidCells => _invalidCellsView;

    public string ErrorSummary => $"Errores: {ErrorCount}/{ErrorLimitValue}";

    public bool IsAutoCompleteOverlayVisible => AutoCompleteSessionState == AutoCompleteSessionState.Running;

    public string AutoCompleteProgressText => $"{AutoCompleteQueueCompleted}/{AutoCompleteQueueTotal}";

    public string AutoCompleteCurrentDigitText =>
        AutoCompleteCurrentDigit is >= 1 and <= 9
            ? AutoCompleteCurrentDigit.ToString()
            : "-";

    partial void OnSelectedCellChanged(int value)
    {
        NotifyBoardChanged();
        UpdateActionCommandStates();
    }

    partial void OnSelectedNumberChanged(int value)
    {
        if (value is >= 1 and <= 9 && IsDeleteMode)
        {
            IsDeleteMode = false;
        }

        UpdateNumberOptions();
        NotifyBoardChanged();
    }

    partial void OnErrorCountChanged(int value)
    {
        OnPropertyChanged(nameof(ErrorSummary));
    }

    partial void OnAutoCompleteEnabledChanged(bool value)
    {
        _themePreferenceStore?.SaveAutoCompleteEnabled(value);

        if (!value)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Idle;
            ClearAutoCompleteQueue();
        }

        EvaluateAutoCompleteSession();

        if (IsGameFinished || IsAwaitingDefeatDecision || AutoCompleteSessionState == AutoCompleteSessionState.Running)
        {
            return;
        }

        StatusMessage = value
            ? "Autocompletado activado."
            : "Autocompletado desactivado.";
    }

    partial void OnIsGameFinishedChanged(bool value)
    {
        if (value && IsDeleteMode)
        {
            IsDeleteMode = false;
        }

        if (value)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Finished;
            ClearAutoCompleteQueue(resetProgress: false);
        }

        EvaluateAutoCompleteSession();
        UpdateActionCommandStates();
    }

    partial void OnAutoCompleteSessionStateChanged(AutoCompleteSessionState value)
    {
        OnPropertyChanged(nameof(IsAutoCompleteOverlayVisible));
        UpdateActionCommandStates();
    }

    partial void OnIsAutoCompleteTriggerReadyChanged(bool value)
    {
        UpdateActionCommandStates();
    }

    partial void OnAutoCompleteQueueTotalChanged(int value)
    {
        OnPropertyChanged(nameof(AutoCompleteProgressText));
    }

    partial void OnAutoCompleteQueueCompletedChanged(int value)
    {
        OnPropertyChanged(nameof(AutoCompleteProgressText));
    }

    partial void OnAutoCompleteCurrentDigitChanged(int value)
    {
        OnPropertyChanged(nameof(AutoCompleteCurrentDigitText));
    }

    partial void OnAutoCompleteStartsChanged(int value)
    {
        PersistAutoCompleteTelemetry();
    }

    partial void OnAutoCompleteCancelsChanged(int value)
    {
        PersistAutoCompleteTelemetry();
    }

    partial void OnAutoCompleteFilledCellsChanged(int value)
    {
        PersistAutoCompleteTelemetry();
    }

    partial void OnIsDeleteModeChanged(bool value)
    {
        if (IsGameFinished && value)
        {
            IsDeleteMode = false;
            return;
        }

        if (value)
        {
            SelectedNumber = 0;
            StatusMessage = "Modo borrar activado.";
        }
    }

    partial void OnThemeModeChanged(ThemeMode value)
    {
        ApplyThemeSelection(value, persistSelection: true);
    }

    partial void OnSelectedDifficultyTierChanged(DifficultyTier value)
    {
        DifficultyLabel = ToDifficultyLabel(value);
        _themePreferenceStore?.SaveDifficultyTier(value);
        EvaluateAutoCompleteSession();
    }

    [RelayCommand]
    private void NewPuzzle()
    {
        if (_puzzleProvider is null)
        {
            LoadPuzzle(SudokuDefaults.SamplePuzzle, null, null);
            StatusMessage = "Nuevo puzzle cargado (muestra local).";
            return;
        }

        var nextPuzzle = _puzzleProvider.GetNext(SelectedDifficultyTier);
        if (nextPuzzle is null)
        {
            StatusMessage = $"No hay puzzles disponibles para {ToDifficultyLabel(SelectedDifficultyTier)}.";
            return;
        }

        LoadPuzzle(nextPuzzle.Puzzle, nextPuzzle.Solution, nextPuzzle.TimeThresholds);
        DifficultyLabel = ToDifficultyLabel(nextPuzzle.DifficultyTier);
        StatusMessage = $"Nuevo puzzle cargado ({DifficultyLabel}).";
    }

    [RelayCommand]
    private void SwitchToLan()
    {
        IsLanMode = true;
        StatusMessage = "Modo LAN activo.";
    }

    [RelayCommand]
    private void SwitchToCloud()
    {
        IsLanMode = false;
        StatusMessage = "Modo Cloud activo. Requiere login.";
    }

    [RelayCommand]
    private void HostLan()
    {
        StatusMessage = "Servidor LAN listo en http://localhost:5055.";
    }

    [RelayCommand]
    private void QuickMatch()
    {
        StatusMessage = $"Buscando rival para {PlayerEmail}...";
    }

    [RelayCommand]
    private void SelectNumber(int number)
    {
        if (IsGameFinished)
        {
            return;
        }

        if (number is < 1 or > 9)
        {
            return;
        }

        SelectedCell = -1;
        SelectedNumber = number;
    }

    [RelayCommand(CanExecute = nameof(CanUndoMove))]
    private void UndoMove()
    {
        if (IsGameFinished || _moveHistory.Count == 0)
        {
            return;
        }

        var previous = _moveHistory.Pop();
        _cells[previous.Index] = previous.PreviousValue;

        RecalculateState();
        UpdateNumberOptions();
        EvaluateAutoCompleteSession();
        NotifyBoardChanged();
        UpdateActionCommandStates();
        StatusMessage = "Ultima jugada deshecha.";
    }

    [RelayCommand]
    private void ToggleAutoComplete()
    {
        if (IsGameFinished)
        {
            return;
        }

        AutoCompleteEnabled = !AutoCompleteEnabled;
    }

    private bool TryStartAutoCompleteSession(bool startedAutomatically)
    {
        if (IsGameFinished || !IsAutoCompleteTriggerReady || AutoCompleteSessionState == AutoCompleteSessionState.Running)
        {
            return false;
        }

        var queue = BuildAutoCompleteQueue();
        if (queue.Count == 0)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Cancelled;
            IsAutoCompleteTriggerReady = false;
            StatusMessage = "No se encontraron jugadas para autocompletar en esta partida.";
            return false;
        }

        ClearAutoCompleteQueue();
        foreach (var step in queue)
        {
            _autoCompleteQueue.Enqueue(step);
        }

        AutoCompleteQueueTotal = _autoCompleteQueue.Count;
        AutoCompleteQueueCompleted = 0;
        AutoCompleteCurrentDigit = _autoCompleteQueue.Peek().Value;
        AutoCompleteSessionState = AutoCompleteSessionState.Running;
        IsAutoCompleteTriggerReady = false;
        AutoCompleteStarts++;
        RecordAutoCompleteDiagnostic("start");
        StatusMessage = startedAutomatically
            ? $"Autocompletado iniciado automaticamente ({AutoCompleteProgressText})."
            : $"Autocompletado en progreso ({AutoCompleteProgressText}).";
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanCancelAutoCompleteSession))]
    private void CancelAutoCompleteSession()
    {
        if (IsGameFinished ||
            AutoCompleteSessionState is AutoCompleteSessionState.Cancelled or AutoCompleteSessionState.Finished)
        {
            return;
        }

        AutoCompleteCancels++;
        ClearAutoCompleteQueue();
        AutoCompleteSessionState = AutoCompleteSessionState.Cancelled;
        EvaluateAutoCompleteSession();
        RecordAutoCompleteDiagnostic("cancel");
        StatusMessage = "Autocompletado cancelado para esta partida.";
    }

    public void ProcessAutoCompleteTick()
    {
        if (AutoCompleteSessionState != AutoCompleteSessionState.Running || IsGameFinished)
        {
            return;
        }

        while (_autoCompleteQueue.Count > 0)
        {
            var step = _autoCompleteQueue.Dequeue();
            if (!IsCellEditable(step.Index) || _cells[step.Index] is not null)
            {
                continue;
            }

            AutoCompleteCurrentDigit = step.Value;
            ApplyCellEdit(step.Index, step.Value, saveHistory: true);
            AutoCompleteQueueCompleted++;
            AutoCompleteFilledCells++;

            if (IsGameFinished || AutoCompleteSessionState != AutoCompleteSessionState.Running)
            {
                ClearAutoCompleteQueue(resetProgress: false);
                return;
            }

            if (_autoCompleteQueue.Count > 0)
            {
                AutoCompleteCurrentDigit = _autoCompleteQueue.Peek().Value;
                StatusMessage = $"Autocompletado en progreso ({AutoCompleteProgressText}).";
            }
            else
            {
                AutoCompleteCurrentDigit = 0;
                AutoCompleteSessionState = AutoCompleteSessionState.Finished;
                EvaluateAutoCompleteSession();
                RecordAutoCompleteDiagnostic("finish");
                StatusMessage = "Autocompletado de sesion finalizado.";
            }

            return;
        }

        AutoCompleteCurrentDigit = 0;
        AutoCompleteSessionState = AutoCompleteSessionState.Finished;
        EvaluateAutoCompleteSession();
        RecordAutoCompleteDiagnostic("finish");
        StatusMessage = "Autocompletado de sesion finalizado.";
    }

    public void TickClock()
    {
        if (IsGameFinished || IsAwaitingDefeatDecision)
        {
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - _gameStartedUtc - _pausedClockDuration;
        ElapsedTimeText = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
    }

    public void PauseClock()
    {
        if (_clockPausedAtUtc is not null)
        {
            return;
        }

        _clockPausedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ResumeClock()
    {
        if (_clockPausedAtUtc is null)
        {
            return;
        }

        _pausedClockDuration += DateTimeOffset.UtcNow - _clockPausedAtUtc.Value;
        _clockPausedAtUtc = null;
        TickClock();
    }

    public void SelectCell(int index)
    {
        if (IsGameFinished)
        {
            return;
        }

        if (index == -1)
        {
            SelectedCell = -1;
            return;
        }

        if (index is < 0 or >= 81)
        {
            return;
        }

        SelectedCell = index;

        if (IsDeleteMode)
        {
            return;
        }

        if (SelectedNumber is >= 1 and <= 9 && IsCellEditable(index))
        {
            ApplyCellEdit(index, SelectedNumber, saveHistory: true);
        }
    }

    public void DeleteCellFromTool(int index)
    {
        if (IsGameFinished || index is < 0 or >= 81)
        {
            return;
        }

        SelectedCell = index;
        ApplyCellEdit(index, null, saveHistory: true);
    }

    public void ApplyCellEdit(int index, int? value, bool saveHistory = true)
    {
        if (IsGameFinished || index is < 0 or >= 81)
        {
            return;
        }

        if (!IsCellEditable(index))
        {
            StatusMessage = "Las celdas fijas no se pueden modificar.";
            return;
        }

        if (value is < 1 or > 9)
        {
            value = null;
        }

        var previousValue = _cells[index];
        if (previousValue == value)
        {
            return;
        }

        if (saveHistory)
        {
            _moveHistory.Push(new MoveEntry(index, previousValue));
        }

        var nowUtc = DateTimeOffset.UtcNow;
        _cells[index] = value;
        if (value is >= 1 and <= 9)
        {
            SelectedNumber = value.Value;
        }

        RecalculateState();
        var isInvalidMove = _invalidCells[index] && value is not null;
        var completionEventArgs = BuildCompletionEvent(index, value, isInvalidMove);

        if (completionEventArgs is not null)
        {
            CompletionUnitsAchieved?.Invoke(this, completionEventArgs);
        }

        if (isInvalidMove)
        {
            ApplyScoreMove(new ScoreMoveInput(
                SelectedDifficultyTier,
                IsCorrectEditableFill: false,
                SecondsSinceLastCorrectFill: 0,
                CompletedUnitsCount: 0,
                NumberUsedUp: false));

            ErrorCount = Math.Min(ErrorCount + 1, ErrorLimitValue);
            StatusMessage = "Movimiento invalido: ese numero no encaja en la celda.";
            TriggerDefeatThresholdIfNeeded();
        }
        else if (value is null)
        {
            StatusMessage = "Celda borrada.";
        }
        else if (value is int placedDigit)
        {
            var completedUnitsCount = CountCompletedUnits(completionEventArgs);
            var secondsSinceLastCorrectFill = (int)Math.Max(0, (nowUtc - _lastCorrectFillAtUtc).TotalSeconds);
            _lastCorrectFillAtUtc = nowUtc;
            var numberUsedUp = IsNumberExhausted(placedDigit);
            ApplyScoreMove(new ScoreMoveInput(
                SelectedDifficultyTier,
                IsCorrectEditableFill: true,
                SecondsSinceLastCorrectFill: secondsSinceLastCorrectFill,
                CompletedUnitsCount: completedUnitsCount,
                NumberUsedUp: numberUsedUp));
            StatusMessage = "Movimiento aplicado.";
        }

        if (!isInvalidMove && IsSolved())
        {
            ApplyFinishScore();
            AutoCompleteSessionState = AutoCompleteSessionState.Finished;
            IsGameFinished = true;
            IsVictory = true;
            StatusMessage = "Puzzle completado.";
            GameWon?.Invoke(this, EventArgs.Empty);
        }

        UpdateNumberOptions();
        EvaluateAutoCompleteSession();
        NotifyBoardChanged();
        UpdateActionCommandStates();
    }

    private void ApplyScoreMove(ScoreMoveInput input)
    {
        var result = _scoreEngine.ScoreMove(input);
        _scoreAccumulator.AddMove(result);
        RefreshScores();
    }

    private void ApplyFinishScore()
    {
        var elapsedSeconds = GetElapsedSeconds();
        var finishResult = _scoreEngine.ScoreFinish(new ScoreFinishInput(
            SelectedDifficultyTier,
            elapsedSeconds,
            ErrorCount,
            ErrorLimitValue,
            IsPerfectRun(),
            _currentPuzzleTimeThresholds));
        _scoreAccumulator.AddFinish(finishResult);
        RefreshScores();
        LastVictorySummary = BuildVictorySummary(elapsedSeconds);
    }

    private void RefreshScores()
    {
        OldScore = _scoreAccumulator.OldScore;
        NewScore = _scoreAccumulator.NewScore;
        Score = _scoreEngine.SelectFinalScore(OldScore, NewScore, ActiveScoreVersion);
    }

    private int GetElapsedSeconds()
    {
        var elapsed = DateTimeOffset.UtcNow - _gameStartedUtc - _pausedClockDuration;
        return Math.Max(0, (int)elapsed.TotalSeconds);
    }

    private bool IsPerfectRun()
    {
        return ErrorCount == 0;
    }

    private bool IsNumberExhausted(int digit)
    {
        return _cells.Count(value => value == digit) == 9;
    }

    private static int CountCompletedUnits(CompletionUnitsEventArgs? args)
    {
        if (args is null)
        {
            return 0;
        }

        var count = 0;
        if (args.RowCompleted)
        {
            count++;
        }

        if (args.ColumnCompleted)
        {
            count++;
        }

        if (args.BoxCompleted)
        {
            count++;
        }

        return count;
    }

    private static IReadOnlyList<int> NormalizeTimeThresholds(IReadOnlyList<int>? thresholds)
    {
        if (thresholds is null || thresholds.Count != 4 || thresholds.Any(threshold => threshold <= 0))
        {
            return [150, 210, 300, 420];
        }

        var ordered = thresholds.ToArray();
        Array.Sort(ordered);
        return ordered;
    }

    private VictorySummary BuildVictorySummary(int elapsedSeconds)
    {
        var elapsedTimeText = $"{elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}";
        var useNewScore = ActiveScoreVersion == ScoreVersion.New;
        return new VictorySummary(
            FinalScore: Score,
            OldScore: OldScore,
            NewScore: NewScore,
            ScoreVersion: ActiveScoreVersion,
            DifficultyLabel: DifficultyLabel,
            ElapsedTimeText: elapsedTimeText,
            ElapsedSeconds: elapsedSeconds,
            ErrorCount: ErrorCount,
            IsPerfect: IsPerfectRun(),
            MoveFillScore: useNewScore ? _scoreAccumulator.NewMoveFillScore : _scoreAccumulator.OldMoveFillScore,
            ClearScore: useNewScore ? _scoreAccumulator.NewClearScore : _scoreAccumulator.OldClearScore,
            NumberUseUpScore: useNewScore ? _scoreAccumulator.NewNumberUseUpScore : _scoreAccumulator.OldNumberUseUpScore,
            TimeBonusScore: useNewScore ? _scoreAccumulator.NewFinishTimeScore : _scoreAccumulator.OldFinishTimeScore,
            ErrorBonusScore: useNewScore ? _scoreAccumulator.NewFinishErrorScore : _scoreAccumulator.OldFinishErrorScore,
            PerfectBonusScore: useNewScore ? _scoreAccumulator.NewFinishPerfectScore : _scoreAccumulator.OldFinishPerfectScore,
            PenaltyScore: useNewScore ? 0 : _scoreAccumulator.OldPenaltyScore);
    }

    private bool CanUndoMove() => !IsGameFinished && _moveHistory.Count > 0;

    public void ContinueAfterDefeatWarning()
    {
        if (IsGameFinished)
        {
            return;
        }

        ErrorCount = Math.Max(0, ErrorCount - 1);
        _isDefeatDialogOpen = false;
        IsAwaitingDefeatDecision = false;
        EvaluateAutoCompleteSession();
        StatusMessage = "Continuaste la partida tras alcanzar el limite de errores.";
    }

    public void MarkDefeatAndStop()
    {
        if (IsGameFinished)
        {
            return;
        }

        _isDefeatDialogOpen = false;
        IsAwaitingDefeatDecision = false;
        AutoCompleteSessionState = AutoCompleteSessionState.Finished;
        IsGameFinished = true;
        IsVictory = false;
        StatusMessage = "Partida marcada como derrota.";
        GameLost?.Invoke(this, EventArgs.Empty);
        UpdateActionCommandStates();
    }

    private bool CanCancelAutoCompleteSession()
    {
        return !IsGameFinished && AutoCompleteSessionState == AutoCompleteSessionState.Running;
    }

    private void LoadPuzzle(string puzzle, string? solution, IReadOnlyList<int>? timeThresholds)
    {
        var board = SudokuBoard.CreateFromString(puzzle);
        _resolvedSolution = ResolveSolutionDigits(puzzle, solution);
        _currentPuzzleTimeThresholds = NormalizeTimeThresholds(timeThresholds);
        for (var i = 0; i < 81; i++)
        {
            _cells[i] = board.Cells[i];
            _givens[i] = board.Givens[i];
            _editableCells[i] = !board.Givens[i];
            _invalidCells[i] = false;
        }

        _moveHistory.Clear();
        SelectedCell = -1;
        SelectedNumber = 0;
        AutoCompleteSessionState = AutoCompleteSessionState.Idle;
        IsAutoCompleteTriggerReady = false;
        AutoCompleteRemainingToSolve = 0;
        ClearAutoCompleteQueue();
        _scoreAccumulator.Reset();
        OldScore = 0;
        NewScore = 0;
        Score = 0;
        LastVictorySummary = null;
        ErrorCount = 0;
        IsDeleteMode = false;
        IsGameFinished = false;
        IsVictory = false;
        IsAwaitingDefeatDecision = false;
        _isDefeatDialogOpen = false;
        _gameStartedUtc = DateTimeOffset.UtcNow;
        _lastCorrectFillAtUtc = _gameStartedUtc;
        _clockPausedAtUtc = null;
        _pausedClockDuration = TimeSpan.Zero;
        TickClock();
        RecalculateState();
        UpdateNumberOptions();
        EvaluateAutoCompleteSession();
        NotifyBoardChanged();
        UpdateActionCommandStates();
    }

    private void RecalculateState()
    {
        Array.Fill(_invalidCells, false);

        for (var i = 0; i < 81; i++)
        {
            if (_cells[i] is int value && !IsPlacementValid(i, value))
            {
                _invalidCells[i] = true;
            }
        }
    }

    private bool IsPlacementValid(int index, int value)
    {
        var row = index / 9;
        var col = index % 9;

        for (var i = 0; i < 9; i++)
        {
            var rowIndex = (row * 9) + i;
            if (rowIndex != index && _cells[rowIndex] == value)
            {
                return false;
            }

            var colIndex = (i * 9) + col;
            if (colIndex != index && _cells[colIndex] == value)
            {
                return false;
            }
        }

        var boxStartRow = (row / 3) * 3;
        var boxStartCol = (col / 3) * 3;
        for (var r = boxStartRow; r < boxStartRow + 3; r++)
        {
            for (var c = boxStartCol; c < boxStartCol + 3; c++)
            {
                var boxIndex = (r * 9) + c;
                if (boxIndex != index && _cells[boxIndex] == value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsSolved()
    {
        return _cells.All(v => v is >= 1 and <= 9) && _invalidCells.All(x => !x);
    }

    private void UpdateNumberOptions()
    {
        for (var number = 1; number <= 9; number++)
        {
            var used = _cells.Count(x => x == number);
            var remaining = Math.Max(0, 9 - used);
            var option = NumberOptions[number - 1];
            option.RemainingCount = remaining;
            option.IsAvailable = remaining > 0;
            option.IsSelected = SelectedNumber == number;
            option.IsDimmed = SelectedNumber > 0 && !option.IsSelected;
        }
    }

    private void UpdateActionCommandStates()
    {
        UndoMoveCommand.NotifyCanExecuteChanged();
        CancelAutoCompleteSessionCommand.NotifyCanExecuteChanged();
    }

    private void NotifyBoardChanged()
    {
        RefreshBoardViews();
        OnPropertyChanged(nameof(Cells));
        OnPropertyChanged(nameof(GivenCells));
        OnPropertyChanged(nameof(EditableCells));
        OnPropertyChanged(nameof(InvalidCells));
        OnPropertyChanged(nameof(SelectedNumber));
    }

    private void RefreshBoardViews()
    {
        _cellsView = _cells.ToArray();
        _givenCellsView = _givens.ToArray();
        _editableCellsView = _editableCells.ToArray();
        _invalidCellsView = _invalidCells.ToArray();
    }

    private bool IsCellEditable(int index)
    {
        return index is >= 0 and < 81 && _editableCells[index];
    }

    private void TriggerDefeatThresholdIfNeeded()
    {
        if (ErrorCount < ErrorLimitValue || _isDefeatDialogOpen || IsGameFinished)
        {
            return;
        }

        _isDefeatDialogOpen = true;
        IsAwaitingDefeatDecision = true;
        EvaluateAutoCompleteSession();
        DefeatThresholdReached?.Invoke(this, EventArgs.Empty);
    }

    private void EvaluateAutoCompleteSession()
    {
        AutoCompleteRemainingToSolve = CountRemainingEditableCells();

        var policyDecision = _autoCompletePolicyEvaluator?.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled,
            IsGameFinished,
            IsAwaitingDefeatDecision,
            AutoCompleteSessionState == AutoCompleteSessionState.Cancelled,
            AutoCompleteRemainingToSolve,
            SelectedDifficultyTier));

        AutoCompleteTriggerMinRemaining = policyDecision?.MinRemainingToTrigger ?? 5;
        AutoCompleteTriggerMaxRemaining = policyDecision?.MaxRemainingToTrigger ?? 9;
        AutoCompleteTickIntervalMilliseconds =
            policyDecision?.TickIntervalMilliseconds ?? DefaultAutoCompleteTickIntervalMilliseconds;

        if (AutoCompleteSessionState == AutoCompleteSessionState.Running)
        {
            IsAutoCompleteTriggerReady = false;
            return;
        }

        var shouldPrompt = policyDecision?.ShouldPrompt ??
                           (AutoCompleteEnabled &&
                            !IsGameFinished &&
                            !IsAwaitingDefeatDecision &&
                            AutoCompleteSessionState is not AutoCompleteSessionState.Cancelled &&
                            AutoCompleteRemainingToSolve is >= 5 and <= 9);

        IsAutoCompleteTriggerReady = shouldPrompt;

        if (AutoCompleteSessionState == AutoCompleteSessionState.Finished)
        {
            return;
        }

        if (shouldPrompt &&
            (AutoCompleteSessionState == AutoCompleteSessionState.Idle ||
             AutoCompleteSessionState == AutoCompleteSessionState.Prompted))
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Prompted;
            _ = TryStartAutoCompleteSession(startedAutomatically: true);
            return;
        }

        if (!shouldPrompt && AutoCompleteSessionState == AutoCompleteSessionState.Prompted)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Idle;
        }
    }

    private IReadOnlyList<AutoCompleteStep> BuildAutoCompleteQueue()
    {
        if (_resolvedSolution.Length != 81)
        {
            return Array.Empty<AutoCompleteStep>();
        }

        var queue = new List<AutoCompleteStep>();
        for (var index = 0; index < 81; index++)
        {
            if (!_editableCells[index] || _cells[index] is not null)
            {
                continue;
            }

            var value = _resolvedSolution[index];
            if (value is < 1 or > 9)
            {
                continue;
            }

            queue.Add(new AutoCompleteStep(index, value));
        }

        return queue;
    }

    private void ClearAutoCompleteQueue(bool resetProgress = true)
    {
        _autoCompleteQueue.Clear();
        AutoCompleteCurrentDigit = 0;
        if (resetProgress)
        {
            AutoCompleteQueueTotal = 0;
            AutoCompleteQueueCompleted = 0;
        }
    }

    private int CountRemainingEditableCells()
    {
        var remaining = 0;
        for (var i = 0; i < 81; i++)
        {
            if (_editableCells[i] && _cells[i] is null)
            {
                remaining++;
            }
        }

        return remaining;
    }

    private static int[] ResolveSolutionDigits(string puzzle, string? solution)
    {
        if (TryParseSolution(solution, out var parsed))
        {
            return parsed;
        }

        return TrySolvePuzzle(puzzle) ?? [];
    }

    private static bool TryParseSolution(string? solution, out int[] parsed)
    {
        if (string.IsNullOrWhiteSpace(solution) ||
            solution.Length != 81 ||
            solution.Any(character => character is < '1' or > '9'))
        {
            parsed = [];
            return false;
        }

        parsed = new int[81];
        for (var i = 0; i < solution.Length; i++)
        {
            parsed[i] = solution[i] - '0';
        }

        return true;
    }

    private static int[]? TrySolvePuzzle(string puzzle)
    {
        if (string.IsNullOrWhiteSpace(puzzle) || puzzle.Length != 81)
        {
            return null;
        }

        var board = new int[81];
        for (var i = 0; i < puzzle.Length; i++)
        {
            var character = puzzle[i];
            if (character is >= '1' and <= '9')
            {
                board[i] = character - '0';
            }
            else
            {
                board[i] = 0;
            }
        }

        return SolveRecursive(board) ? board : null;
    }

    private static bool SolveRecursive(int[] board)
    {
        var targetIndex = -1;
        var bestMask = 0;
        var bestCount = 10;

        for (var i = 0; i < board.Length; i++)
        {
            if (board[i] != 0)
            {
                continue;
            }

            var candidateMask = BuildCandidateMask(board, i);
            var candidateCount = CountBits(candidateMask);
            if (candidateCount == 0)
            {
                return false;
            }

            if (candidateCount >= bestCount)
            {
                continue;
            }

            targetIndex = i;
            bestMask = candidateMask;
            bestCount = candidateCount;
            if (bestCount == 1)
            {
                break;
            }
        }

        if (targetIndex == -1)
        {
            return true;
        }

        for (var digit = 1; digit <= 9; digit++)
        {
            var bit = 1 << digit;
            if ((bestMask & bit) == 0)
            {
                continue;
            }

            board[targetIndex] = digit;
            if (SolveRecursive(board))
            {
                return true;
            }
        }

        board[targetIndex] = 0;
        return false;
    }

    private static int BuildCandidateMask(int[] board, int index)
    {
        var row = index / 9;
        var col = index % 9;
        var usedMask = 0;

        for (var i = 0; i < 9; i++)
        {
            var rowDigit = board[(row * 9) + i];
            if (rowDigit is >= 1 and <= 9)
            {
                usedMask |= 1 << rowDigit;
            }

            var columnDigit = board[(i * 9) + col];
            if (columnDigit is >= 1 and <= 9)
            {
                usedMask |= 1 << columnDigit;
            }
        }

        var rowStart = (row / 3) * 3;
        var colStart = (col / 3) * 3;
        for (var r = rowStart; r < rowStart + 3; r++)
        {
            for (var c = colStart; c < colStart + 3; c++)
            {
                var digit = board[(r * 9) + c];
                if (digit is >= 1 and <= 9)
                {
                    usedMask |= 1 << digit;
                }
            }
        }

        return 1022 & ~usedMask;
    }

    private static int CountBits(int value)
    {
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }

    private sealed record MoveEntry(int Index, int? PreviousValue);

    private void ApplyThemeSelection(ThemeMode mode, bool persistSelection)
    {
        if (_themeManager is null)
        {
            EffectiveThemeMode = mode is ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
        }
        else
        {
            EffectiveThemeMode = _themeManager.ApplyTheme(mode);
        }

        if (persistSelection)
        {
            _themePreferenceStore?.SaveThemeMode(mode);
        }
    }

    private CompletionUnitsEventArgs? BuildCompletionEvent(int index, int? value, bool isInvalidMove)
    {
        if (isInvalidMove || value is not int digit || digit is < 1 or > 9 || index is < 0 or >= 81)
        {
            return null;
        }

        var row = index / 9;
        var col = index % 9;
        var rowDone = IsRowCompleted(row);
        var colDone = IsColumnCompleted(col);
        var boxDone = IsBoxCompleted(row, col);
        if (!rowDone && !colDone && !boxDone)
        {
            return null;
        }

        return new CompletionUnitsEventArgs(index, row, col, rowDone, colDone, boxDone);
    }

    private bool IsRowCompleted(int row)
    {
        var mask = 0;
        for (var col = 0; col < 9; col++)
        {
            var value = _cells[(row * 9) + col];
            if (value is not int digit || digit is < 1 or > 9)
            {
                return false;
            }

            var bit = 1 << digit;
            if ((mask & bit) != 0)
            {
                return false;
            }

            mask |= bit;
        }

        return mask == 1022;
    }

    private bool IsColumnCompleted(int col)
    {
        var mask = 0;
        for (var row = 0; row < 9; row++)
        {
            var value = _cells[(row * 9) + col];
            if (value is not int digit || digit is < 1 or > 9)
            {
                return false;
            }

            var bit = 1 << digit;
            if ((mask & bit) != 0)
            {
                return false;
            }

            mask |= bit;
        }

        return mask == 1022;
    }

    private bool IsBoxCompleted(int row, int col)
    {
        var mask = 0;
        var rowStart = (row / 3) * 3;
        var colStart = (col / 3) * 3;

        for (var r = rowStart; r < rowStart + 3; r++)
        {
            for (var c = colStart; c < colStart + 3; c++)
            {
                var value = _cells[(r * 9) + c];
                if (value is not int digit || digit is < 1 or > 9)
                {
                    return false;
                }

                var bit = 1 << digit;
                if ((mask & bit) != 0)
                {
                    return false;
                }

                mask |= bit;
            }
        }

        return mask == 1022;
    }

    private static string ToDifficultyLabel(DifficultyTier value)
    {
        return value switch
        {
            DifficultyTier.Beginner => "Principiante",
            DifficultyTier.Easy => "Facil",
            DifficultyTier.Medium => "Medio",
            DifficultyTier.Hard => "Dificil",
            DifficultyTier.Expert => "Experto",
            _ => "Medio"
        };
    }

    private sealed record AutoCompleteStep(int Index, int Value);

    private void PersistAutoCompleteTelemetry()
    {
        _themePreferenceStore?.SaveAutoCompleteTelemetry(new AutoCompleteTelemetrySnapshot(
            AutoCompleteStarts,
            AutoCompleteCancels,
            AutoCompleteFilledCells));
    }

    private void RecordAutoCompleteDiagnostic(string eventType)
    {
        _autoCompleteDiagnosticsSink?.Record(new AutoCompleteDiagnosticEvent(
            DateTimeOffset.UtcNow,
            eventType,
            SelectedDifficultyTier.ToString(),
            AutoCompleteRemainingToSolve,
            AutoCompleteQueueCompleted,
            AutoCompleteQueueTotal,
            AutoCompleteTickIntervalMilliseconds));
    }
}

public sealed record CompletionUnitsEventArgs(
    int Index,
    int Row,
    int Column,
    bool RowCompleted,
    bool ColumnCompleted,
    bool BoxCompleted);

public sealed record VictorySummary(
    int FinalScore,
    int OldScore,
    int NewScore,
    ScoreVersion ScoreVersion,
    string DifficultyLabel,
    string ElapsedTimeText,
    int ElapsedSeconds,
    int ErrorCount,
    bool IsPerfect,
    int MoveFillScore,
    int ClearScore,
    int NumberUseUpScore,
    int TimeBonusScore,
    int ErrorBonusScore,
    int PerfectBonusScore,
    int PenaltyScore);

public enum AutoCompleteSessionState
{
    Idle,
    Prompted,
    Running,
    Cancelled,
    Finished
}

public partial class NumberOptionItem(int number) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty]
    private bool _isAvailable = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isDimmed;

    [ObservableProperty]
    private int _remainingCount = 9;
}

internal sealed class ScoreAccumulator
{
    public int OldScore { get; private set; }
    public int NewScore { get; private set; }

    public int OldMoveFillScore { get; private set; }
    public int OldClearScore { get; private set; }
    public int OldNumberUseUpScore { get; private set; }
    public int OldPenaltyScore { get; private set; }
    public int OldFinishPerfectScore { get; private set; }
    public int OldFinishErrorScore { get; private set; }
    public int OldFinishTimeScore { get; private set; }

    public int NewMoveFillScore { get; private set; }
    public int NewClearScore { get; private set; }
    public int NewNumberUseUpScore { get; private set; }
    public int NewFinishPerfectScore { get; private set; }
    public int NewFinishErrorScore { get; private set; }
    public int NewFinishTimeScore { get; private set; }

    public void AddMove(ScoreMoveResult result)
    {
        OldScore = Math.Max(0, OldScore + result.OldDelta);
        NewScore = Math.Max(0, NewScore + result.NewDelta);

        OldMoveFillScore += result.OldFillDelta;
        OldClearScore += result.OldClearDelta;
        OldNumberUseUpScore += result.OldNumberUseUpDelta;
        OldPenaltyScore += result.OldErrorPenaltyDelta;

        NewMoveFillScore += result.NewFillTimeDelta;
        NewClearScore += result.NewClearDelta;
        NewNumberUseUpScore += result.NewNumberUseUpDelta;
    }

    public void AddFinish(ScoreFinishResult result)
    {
        OldScore = Math.Max(0, OldScore + result.OldDelta);
        NewScore = Math.Max(0, NewScore + result.NewDelta);

        OldFinishPerfectScore += result.OldPerfectDelta;
        OldFinishErrorScore += result.OldErrorDelta;
        OldFinishTimeScore += result.OldTimeDelta;

        NewFinishPerfectScore += result.NewPerfectDelta;
        NewFinishErrorScore += result.NewErrorDelta;
        NewFinishTimeScore += result.NewTimeDelta;
    }

    public void Reset()
    {
        OldScore = 0;
        NewScore = 0;

        OldMoveFillScore = 0;
        OldClearScore = 0;
        OldNumberUseUpScore = 0;
        OldPenaltyScore = 0;
        OldFinishPerfectScore = 0;
        OldFinishErrorScore = 0;
        OldFinishTimeScore = 0;

        NewMoveFillScore = 0;
        NewClearScore = 0;
        NewNumberUseUpScore = 0;
        NewFinishPerfectScore = 0;
        NewFinishErrorScore = 0;
        NewFinishTimeScore = 0;
    }
}

