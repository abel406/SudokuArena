using System.Windows;
using System.Windows.Threading;
using SudokuArena.Desktop.Controls;
using SudokuArena.Desktop.Dialogs;
using SudokuArena.Desktop.Theming;
using SudokuArena.Desktop.ViewModels;
using DesktopThemeMode = SudokuArena.Desktop.Theming.ThemeMode;

namespace SudokuArena.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ThemeManager _themeManager;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _autoCompleteTimer;
    private int _lastPlayedIndex = 40;

    public MainWindow(MainViewModel viewModel, ThemeManager themeManager)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _themeManager = themeManager;
        DataContext = viewModel;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => _viewModel.TickClock();

        _autoCompleteTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _autoCompleteTimer.Tick += (_, _) => _viewModel.ProcessAutoCompleteTick();
        UpdateAutoCompleteTimerInterval();

        BoardControl.CellSelected += OnBoardCellSelected;
        BoardControl.CellEdited += OnBoardCellEdited;
        BoardControl.NumberQuickSelected += OnBoardNumberQuickSelected;
        _viewModel.DefeatThresholdReached += OnDefeatThresholdReached;
        _viewModel.GameWon += OnGameWon;
        _viewModel.GameLost += OnGameLost;
        _viewModel.CompletionUnitsAchieved += OnCompletionUnitsAchieved;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _themeManager.ThemeApplied += OnThemeApplied;
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
        _autoCompleteTimer.Stop();
        _viewModel.DefeatThresholdReached -= OnDefeatThresholdReached;
        _viewModel.GameWon -= OnGameWon;
        _viewModel.GameLost -= OnGameLost;
        _viewModel.CompletionUnitsAchieved -= OnCompletionUnitsAchieved;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _themeManager.ThemeApplied -= OnThemeApplied;
    }

    private void OnBoardCellSelected(object? sender, int selectedCell)
    {
        if (selectedCell == -1)
        {
            _viewModel.SelectCell(-1);
            return;
        }

        _lastPlayedIndex = selectedCell;

        if (_viewModel.IsDeleteMode)
        {
            _viewModel.DeleteCellFromTool(selectedCell);
            return;
        }

        _viewModel.SelectCell(selectedCell);
    }

    private void OnBoardCellEdited(object? sender, CellEditedEventArgs e)
    {
        _lastPlayedIndex = e.Index;
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

    private async void OnGameWon(object? sender, EventArgs e)
    {
        _viewModel.PauseClock();
        _clockTimer.Stop();
        _autoCompleteTimer.Stop();
        BoardControl.StartVictoryAnimation(_lastPlayedIndex);
        await Task.Delay(700);

        if (_viewModel.LastVictorySummary is null)
        {
            return;
        }

        var dialog = new VictoryWindow(_viewModel.LastVictorySummary)
        {
            Owner = this
        };
        _ = dialog.ShowDialog();
    }

    private void OnGameLost(object? sender, EventArgs e)
    {
        _viewModel.PauseClock();
        _clockTimer.Stop();
        _autoCompleteTimer.Stop();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.AutoCompleteSessionState))
        {
            if (_viewModel.AutoCompleteSessionState == AutoCompleteSessionState.Running)
            {
                UpdateAutoCompleteTimerInterval();
                if (!_autoCompleteTimer.IsEnabled)
                {
                    _autoCompleteTimer.Start();
                }
            }
            else
            {
                _autoCompleteTimer.Stop();
            }

            return;
        }

        if (e.PropertyName == nameof(MainViewModel.AutoCompleteTickIntervalMilliseconds))
        {
            UpdateAutoCompleteTimerInterval();
            return;
        }

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

    private void OnThemeApplied(DesktopThemeMode _)
    {
        BoardControl.InvalidateVisual();
    }

    private void OnCompletionUnitsAchieved(object? sender, CompletionUnitsEventArgs e)
    {
        BoardControl.StartCompletionAnimation(e.Index, e.RowCompleted, e.ColumnCompleted, e.BoxCompleted);
    }

    private void UpdateAutoCompleteTimerInterval()
    {
        var milliseconds = Math.Clamp(_viewModel.AutoCompleteTickIntervalMilliseconds, 120, 2000);
        _autoCompleteTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
    }
}
