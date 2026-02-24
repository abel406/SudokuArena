using System.Windows;
using System.Windows.Threading;
using SudokuArena.Desktop.Controls;
using SudokuArena.Desktop.Dialogs;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _clockTimer;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => _viewModel.TickClock();

        BoardControl.CellSelected += OnBoardCellSelected;
        BoardControl.CellEdited += OnBoardCellEdited;
        BoardControl.NumberQuickSelected += OnBoardNumberQuickSelected;
        _viewModel.DefeatThresholdReached += OnDefeatThresholdReached;
        _viewModel.GameWon += OnGameWon;
        _viewModel.GameLost += OnGameLost;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BoardControl.Focus();
        _viewModel.TickClock();
        _clockTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _clockTimer.Stop();
        _viewModel.DefeatThresholdReached -= OnDefeatThresholdReached;
        _viewModel.GameWon -= OnGameWon;
        _viewModel.GameLost -= OnGameLost;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnBoardCellSelected(object? sender, int selectedCell)
    {
        _viewModel.SelectCell(selectedCell);
    }

    private void OnBoardCellEdited(object? sender, CellEditedEventArgs e)
    {
        _viewModel.ApplyCellEdit(e.Index, e.Value, saveHistory: true);
    }

    private void OnBoardNumberQuickSelected(object? sender, int number)
    {
        if (_viewModel.SelectNumberCommand.CanExecute(number))
        {
            _viewModel.SelectNumberCommand.Execute(number);
        }
    }

    private void OnDefeatThresholdReached(object? sender, EventArgs e)
    {
        _viewModel.PauseClock();
        _clockTimer.Stop();

        var dialog = new DefeatWindow
        {
            Owner = this
        };
        _ = dialog.ShowDialog();

        if (dialog.ContinuePlaying)
        {
            _viewModel.ContinueAfterDefeatWarning();
            _viewModel.ResumeClock();
            if (!_clockTimer.IsEnabled)
            {
                _clockTimer.Start();
            }
            return;
        }

        _viewModel.MarkDefeatAndStop();
    }

    private void OnGameWon(object? sender, EventArgs e)
    {
        _viewModel.PauseClock();
        _clockTimer.Stop();

        var dialog = new VictoryWindow(_viewModel.Score, _viewModel.DifficultyLabel)
        {
            Owner = this
        };
        _ = dialog.ShowDialog();
    }

    private void OnGameLost(object? sender, EventArgs e)
    {
        _viewModel.PauseClock();
        _clockTimer.Stop();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsGameFinished))
        {
            return;
        }

        if (!_viewModel.IsGameFinished && !_clockTimer.IsEnabled)
        {
            _viewModel.ResumeClock();
            _viewModel.TickClock();
            _clockTimer.Start();
        }
    }
}
