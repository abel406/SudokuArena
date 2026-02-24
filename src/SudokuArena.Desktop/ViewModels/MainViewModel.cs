using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SudokuArena.Domain.Models;

namespace SudokuArena.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private SudokuBoard _board = SudokuBoard.CreateFromString(SudokuDefaults.SamplePuzzle);

    [ObservableProperty]
    private string _playerEmail = "player1@gmail.com";

    [ObservableProperty]
    private string _opponentEmail = "player2@gmail.com";

    [ObservableProperty]
    private int _selectedCell = -1;

    [ObservableProperty]
    private string _statusMessage = "Listo para duelo LAN.";

    [ObservableProperty]
    private bool _isLanMode = true;

    public IReadOnlyList<int?> Cells => _board.Cells;

    public IReadOnlyList<bool> GivenCells => _board.Givens;

    [RelayCommand]
    private void NewPuzzle()
    {
        _board = SudokuBoard.CreateFromString(SudokuDefaults.SamplePuzzle);
        SelectedCell = -1;
        StatusMessage = "Nuevo puzzle cargado.";
        NotifyBoardChanged();
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

    public void SelectCell(int index)
    {
        SelectedCell = index;
    }

    public void ApplyCellEdit(int index, int? value)
    {
        if (!_board.TrySetCell(index, value, out var reason))
        {
            StatusMessage = reason ?? "Movimiento inválido.";
            return;
        }

        StatusMessage = _board.IsComplete ? "Puzzle completado." : "Movimiento aplicado.";
        NotifyBoardChanged();
    }

    private void NotifyBoardChanged()
    {
        OnPropertyChanged(nameof(Cells));
        OnPropertyChanged(nameof(GivenCells));
    }
}
