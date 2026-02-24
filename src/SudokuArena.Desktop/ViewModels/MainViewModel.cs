using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SudokuArena.Domain.Models;

namespace SudokuArena.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int ErrorLimitValue = 3;

    private readonly int?[] _cells = new int?[81];
    private readonly bool[] _givens = new bool[81];
    private readonly bool[] _invalidCells = new bool[81];
    private readonly Stack<MoveEntry> _moveHistory = new();
    private DateTimeOffset _gameStartedUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset? _clockPausedAtUtc;
    private TimeSpan _pausedClockDuration = TimeSpan.Zero;
    private bool _isDefeatDialogOpen;

    public MainViewModel()
    {
        NumberOptions = new ObservableCollection<NumberOptionItem>(
            Enumerable.Range(1, 9).Select(x => new NumberOptionItem(x)));
        LoadPuzzle(SudokuDefaults.SamplePuzzle);
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
    private int _errorCount;

    [ObservableProperty]
    private string _elapsedTimeText = "00:00";

    [ObservableProperty]
    private bool _autoCompleteEnabled;

    [ObservableProperty]
    private bool _isGameFinished;

    [ObservableProperty]
    private bool _isVictory;

    [ObservableProperty]
    private bool _isAwaitingDefeatDecision;

    public ObservableCollection<NumberOptionItem> NumberOptions { get; }

    public event EventHandler? DefeatThresholdReached;

    public event EventHandler? GameWon;

    public event EventHandler? GameLost;

    public IReadOnlyList<int?> Cells => _cells;

    public IReadOnlyList<bool> GivenCells => _givens;

    public IReadOnlyList<bool> InvalidCells => _invalidCells;

    public string ErrorSummary => $"Errores: {ErrorCount}/{ErrorLimitValue}";

    partial void OnSelectedCellChanged(int value)
    {
        NotifyBoardChanged();
        UpdateActionCommandStates();
    }

    partial void OnSelectedNumberChanged(int value)
    {
        UpdateNumberOptions();
        NotifyBoardChanged();
    }

    partial void OnErrorCountChanged(int value)
    {
        OnPropertyChanged(nameof(ErrorSummary));
    }

    partial void OnAutoCompleteEnabledChanged(bool value)
    {
        if (IsGameFinished)
        {
            return;
        }

        StatusMessage = value
            ? "Autocompletado activado (pendiente de implementación)."
            : "Autocompletado desactivado.";
    }

    partial void OnIsGameFinishedChanged(bool value)
    {
        UpdateActionCommandStates();
    }

    [RelayCommand]
    private void NewPuzzle()
    {
        LoadPuzzle(SudokuDefaults.SamplePuzzle);
        StatusMessage = "Nuevo puzzle cargado.";
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
        NotifyBoardChanged();
        UpdateActionCommandStates();
        StatusMessage = "Ultima jugada deshecha.";
    }

    [RelayCommand(CanExecute = nameof(CanClearSelectedCell))]
    private void ClearSelectedCell()
    {
        if (IsGameFinished || SelectedCell is < 0 or >= 81 || _givens[SelectedCell])
        {
            return;
        }

        ApplyCellEdit(SelectedCell, null, saveHistory: true);
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

        if (SelectedNumber is >= 1 and <= 9 && !_givens[index])
        {
            ApplyCellEdit(index, SelectedNumber, saveHistory: true);
        }
    }

    public void ApplyCellEdit(int index, int? value, bool saveHistory = true)
    {
        if (IsGameFinished || index is < 0 or >= 81)
        {
            return;
        }

        if (_givens[index])
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

        if (isInvalidMove)
        {
            ErrorCount = Math.Min(ErrorCount + 1, ErrorLimitValue);
            StatusMessage = "Movimiento invalido: ese numero no encaja en la celda.";
            TriggerDefeatThresholdIfNeeded();
        }
        else if (IsSolved())
        {
            Score += 20;
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
        NotifyBoardChanged();
        UpdateActionCommandStates();
    }

    private bool CanUndoMove() => !IsGameFinished && _moveHistory.Count > 0;

    private bool CanClearSelectedCell()
    {
        return !IsGameFinished
               && SelectedCell is >= 0 and < 81
               && !_givens[SelectedCell]
               && _cells[SelectedCell] is not null;
    }

    public void ContinueAfterDefeatWarning()
    {
        if (IsGameFinished)
        {
            return;
        }

        ErrorCount = Math.Max(0, ErrorCount - 1);
        _isDefeatDialogOpen = false;
        IsAwaitingDefeatDecision = false;
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
            _invalidCells[i] = false;
        }

        _moveHistory.Clear();
        SelectedCell = -1;
        SelectedNumber = 0;
        Score = 0;
        ErrorCount = 0;
        AutoCompleteEnabled = false;
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
        ClearSelectedCellCommand.NotifyCanExecuteChanged();
    }

    private void NotifyBoardChanged()
    {
        OnPropertyChanged(nameof(Cells));
        OnPropertyChanged(nameof(GivenCells));
        OnPropertyChanged(nameof(InvalidCells));
        OnPropertyChanged(nameof(SelectedNumber));
    }

    private void TriggerDefeatThresholdIfNeeded()
    {
        if (ErrorCount < ErrorLimitValue || _isDefeatDialogOpen || IsGameFinished)
        {
            return;
        }

        _isDefeatDialogOpen = true;
        IsAwaitingDefeatDecision = true;
        DefeatThresholdReached?.Invoke(this, EventArgs.Empty);
    }

    private sealed record MoveEntry(int Index, int? PreviousValue);
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

