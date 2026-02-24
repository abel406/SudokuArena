using System.Windows;
using SudokuArena.Desktop.Controls;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        BoardControl.CellSelected += OnBoardCellSelected;
        BoardControl.CellEdited += OnBoardCellEdited;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BoardControl.Focus();
    }

    private void OnBoardCellSelected(object? sender, int selectedCell)
    {
        _viewModel.SelectCell(selectedCell);
    }

    private void OnBoardCellEdited(object? sender, CellEditedEventArgs e)
    {
        _viewModel.ApplyCellEdit(e.Index, e.Value);
    }
}
