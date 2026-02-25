using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelCompletionEventTests
{
    [Fact]
    public void ApplyCellEdit_ShouldRaiseCompletionEvent_WhenValidMoveCompletesUnits()
    {
        const string solvedWithSingleGap =
            "534678910" +
            "672195348" +
            "198342567" +
            "859761423" +
            "426853791" +
            "713924856" +
            "961537284" +
            "287419635" +
            "345286179";

        var viewModel = new MainViewModel(solvedWithSingleGap);
        CompletionUnitsEventArgs? captured = null;
        viewModel.CompletionUnitsAchieved += (_, args) => captured = args;

        viewModel.ApplyCellEdit(8, 2, saveHistory: true);

        Assert.NotNull(captured);
        Assert.Equal(8, captured!.Index);
        Assert.True(captured.RowCompleted);
        Assert.True(captured.ColumnCompleted);
        Assert.True(captured.BoxCompleted);
    }

    [Fact]
    public void ApplyCellEdit_ShouldNotRaiseCompletionEvent_WhenMoveIsInvalid()
    {
        var viewModel = new MainViewModel();
        var raisedCount = 0;
        viewModel.CompletionUnitsAchieved += (_, _) => raisedCount++;

        viewModel.ApplyCellEdit(2, 5, saveHistory: true);

        Assert.Equal(0, raisedCount);
    }

    [Fact]
    public void ApplyCellEdit_ShouldNotRaiseCompletionEvent_WhenClearingCell()
    {
        var viewModel = new MainViewModel();
        var raisedCount = 0;
        viewModel.CompletionUnitsAchieved += (_, _) => raisedCount++;

        viewModel.ApplyCellEdit(2, 4, saveHistory: true);
        viewModel.ApplyCellEdit(2, null, saveHistory: true);

        Assert.Equal(0, raisedCount);
    }

    [Fact]
    public void ApplyCellEdit_ShouldNotRaiseCompletionEvent_WhenCellIsGiven()
    {
        var viewModel = new MainViewModel();
        var raisedCount = 0;
        viewModel.CompletionUnitsAchieved += (_, _) => raisedCount++;

        viewModel.ApplyCellEdit(0, 9, saveHistory: true);

        Assert.Equal(0, raisedCount);
    }
}
