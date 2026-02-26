using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Theming;
using SudokuArena.Domain.Models;

namespace SudokuArena.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int ErrorLimitValue = 3;
    private const int AutoCompleteTriggerMinRemaining = 5;
    private const int AutoCompleteTriggerMaxRemaining = 9;

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
    private readonly ThemeManager? _themeManager;
    private readonly IThemePreferenceStore? _themePreferenceStore;
    private readonly IPuzzleProvider? _puzzleProvider;

    public MainViewModel()
        : this(SudokuDefaults.SamplePuzzle, null, null)
    {
    }

    public MainViewModel(string puzzle)
        : this(puzzle, null, null)
    {
    }

    public MainViewModel(ThemeManager themeManager)
        : this(SudokuDefaults.SamplePuzzle, themeManager, null)
    {
    }

    public MainViewModel(ThemeManager themeManager, IThemePreferenceStore themePreferenceStore)
        : this(SudokuDefaults.SamplePuzzle, themeManager, themePreferenceStore)
    {
    }

    public MainViewModel(IPuzzleProvider puzzleProvider)
        : this(SudokuDefaults.SamplePuzzle, null, null, puzzleProvider)
    {
    }

    public MainViewModel(
        ThemeManager themeManager,
        IThemePreferenceStore themePreferenceStore,
        IPuzzleProvider puzzleProvider)
        : this(SudokuDefaults.SamplePuzzle, themeManager, themePreferenceStore, puzzleProvider)
    {
    }

    private MainViewModel(
        string puzzle,
        ThemeManager? themeManager,
        IThemePreferenceStore? themePreferenceStore,
        IPuzzleProvider? puzzleProvider = null)
    {
        _themeManager = themeManager;
        _themePreferenceStore = themePreferenceStore;
        _puzzleProvider = puzzleProvider;
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
        LoadPuzzle(puzzle);
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
    private bool _isDeleteMode;

    [ObservableProperty]
    private bool _isGameFinished;

    [ObservableProperty]
    private bool _isVictory;

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
        }

        EvaluateAutoCompleteSession();

        if (IsGameFinished || IsAwaitingDefeatDecision)
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
        }

        EvaluateAutoCompleteSession();
        UpdateActionCommandStates();
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
    }

    [RelayCommand]
    private void NewPuzzle()
    {
        if (_puzzleProvider is null)
        {
            LoadPuzzle(SudokuDefaults.SamplePuzzle);
            StatusMessage = "Nuevo puzzle cargado (muestra local).";
            return;
        }

        var nextPuzzle = _puzzleProvider.GetNext(SelectedDifficultyTier);
        if (nextPuzzle is null)
        {
            StatusMessage = $"No hay puzzles disponibles para {ToDifficultyLabel(SelectedDifficultyTier)}.";
            return;
        }

        LoadPuzzle(nextPuzzle.Puzzle);
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

    [RelayCommand]
    private void CancelAutoCompleteSession()
    {
        if (IsGameFinished ||
            AutoCompleteSessionState is AutoCompleteSessionState.Cancelled or AutoCompleteSessionState.Finished)
        {
            return;
        }

        AutoCompleteSessionState = AutoCompleteSessionState.Cancelled;
        EvaluateAutoCompleteSession();
        StatusMessage = "Autocompletado cancelado para esta partida.";
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
            ErrorCount = Math.Min(ErrorCount + 1, ErrorLimitValue);
            StatusMessage = "Movimiento invalido: ese numero no encaja en la celda.";
            TriggerDefeatThresholdIfNeeded();
        }
        else if (IsSolved())
        {
            Score += 20;
            AutoCompleteSessionState = AutoCompleteSessionState.Finished;
            IsGameFinished = true;
            IsVictory = true;
            StatusMessage = "Puzzle completado.";
            GameWon?.Invoke(this, EventArgs.Empty);
        }
        else if (value is null)
        {
            StatusMessage = "Celda borrada.";
        }
        else
        {
            Score += 5;
            StatusMessage = "Movimiento aplicado.";
        }

        UpdateNumberOptions();
        EvaluateAutoCompleteSession();
        NotifyBoardChanged();
        UpdateActionCommandStates();
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

    private void LoadPuzzle(string puzzle)
    {
        var board = SudokuBoard.CreateFromString(puzzle);
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
        Score = 0;
        ErrorCount = 0;
        IsDeleteMode = false;
        IsGameFinished = false;
        IsVictory = false;
        IsAwaitingDefeatDecision = false;
        _isDefeatDialogOpen = false;
        _gameStartedUtc = DateTimeOffset.UtcNow;
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

        var shouldPrompt = AutoCompleteEnabled &&
                           !IsGameFinished &&
                           !IsAwaitingDefeatDecision &&
                           AutoCompleteSessionState is not AutoCompleteSessionState.Cancelled &&
                           AutoCompleteRemainingToSolve is >= AutoCompleteTriggerMinRemaining and <= AutoCompleteTriggerMaxRemaining;

        IsAutoCompleteTriggerReady = shouldPrompt;

        if (AutoCompleteSessionState == AutoCompleteSessionState.Finished)
        {
            return;
        }

        if (shouldPrompt && AutoCompleteSessionState == AutoCompleteSessionState.Idle)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Prompted;
            return;
        }

        if (!shouldPrompt && AutoCompleteSessionState == AutoCompleteSessionState.Prompted)
        {
            AutoCompleteSessionState = AutoCompleteSessionState.Idle;
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
}

public sealed record CompletionUnitsEventArgs(
    int Index,
    int Row,
    int Column,
    bool RowCompleted,
    bool ColumnCompleted,
    bool BoxCompleted);

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

