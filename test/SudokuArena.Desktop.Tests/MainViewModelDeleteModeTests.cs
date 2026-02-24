using SudokuArena.Desktop.ViewModels;
using SudokuArena.Domain.Models;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelDeleteModeTests
{
    [Fact]
    public void DeleteTool_ShouldClearEditableCell_AndUndoShouldRestoreValue()
    {
        var viewModel = new MainViewModel();
        const int index = 2;

        viewModel.ApplyCellEdit(index, 4, saveHistory: true);
        viewModel.IsDeleteMode = true;

        viewModel.DeleteCellFromTool(index);

        Assert.Null(viewModel.Cells[index]);
        Assert.True(viewModel.IsDeleteMode);

        Assert.True(viewModel.UndoMoveCommand.CanExecute(null));
        viewModel.UndoMoveCommand.Execute(null);

        Assert.Equal(4, viewModel.Cells[index]);
    }

    [Fact]
    public void DeleteTool_ShouldNotModifyGivenCells()
    {
        var viewModel = new MainViewModel();
        const int givenIndex = 0;
        var originalValue = viewModel.Cells[givenIndex];

        viewModel.IsDeleteMode = true;
        viewModel.DeleteCellFromTool(givenIndex);

        Assert.Equal(originalValue, viewModel.Cells[givenIndex]);
    }

    [Fact]
    public void SelectNumber_ShouldExitDeleteMode()
    {
        var viewModel = new MainViewModel
        {
            IsDeleteMode = true
        };

        viewModel.SelectNumberCommand.Execute(7);

        Assert.False(viewModel.IsDeleteMode);
        Assert.Equal(7, viewModel.SelectedNumber);
    }

    [Fact]
    public void DeleteTool_ShouldOnlyClearClickedCell_WhenMultipleValuesExist()
    {
        var viewModel = new MainViewModel();
        const int firstIndex = 28;
        const int secondIndex = 37;

        viewModel.ApplyCellEdit(firstIndex, 1, saveHistory: true);
        viewModel.ApplyCellEdit(secondIndex, 3, saveHistory: true);
        viewModel.IsDeleteMode = true;

        viewModel.DeleteCellFromTool(secondIndex);

        Assert.Equal(1, viewModel.Cells[firstIndex]);
        Assert.Null(viewModel.Cells[secondIndex]);
    }

    [Fact]
    public void SelectCell_ShouldNotWrite_WhenDeleteModeIsActive()
    {
        var viewModel = new MainViewModel();
        const int index = 2;

        viewModel.SelectNumberCommand.Execute(4);
        viewModel.IsDeleteMode = true;

        viewModel.SelectCell(index);

        Assert.Null(viewModel.Cells[index]);
    }

    [Fact]
    public void DeleteTool_ShouldDeleteRow6Col3_InSamplePuzzle()
    {
        var viewModel = new MainViewModel();
        const int index = 47; // fila 6, columna 3 (1-based)

        Assert.False(viewModel.GivenCells[index]);

        viewModel.ApplyCellEdit(index, 3, saveHistory: true);
        Assert.Equal(3, viewModel.Cells[index]);

        viewModel.IsDeleteMode = true;
        viewModel.DeleteCellFromTool(index);

        Assert.Null(viewModel.Cells[index]);
    }

    [Fact]
    public void DeleteTool_ShouldClearAllNonGivenCells_OnAlmostSolvedBoard()
    {
        var viewModel = new MainViewModel();
        const string solvedBoard = "534678912672195348198342567859761423426853791713924856961537284287419635345286179";
        var editableIndexes = viewModel.EditableCells
            .Select((isEditable, index) => (isEditable, index))
            .Where(x => x.isEditable)
            .Select(x => x.index)
            .ToArray();

        Assert.NotEmpty(editableIndexes);

        var keepEmptyIndex = editableIndexes[^1];
        foreach (var index in editableIndexes)
        {
            if (index == keepEmptyIndex)
            {
                continue;
            }

            viewModel.ApplyCellEdit(index, solvedBoard[index] - '0', saveHistory: true);
        }

        Assert.False(viewModel.IsGameFinished);
        viewModel.IsDeleteMode = true;

        foreach (var index in editableIndexes)
        {
            viewModel.DeleteCellFromTool(index);
        }

        foreach (var index in editableIndexes)
        {
            Assert.Null(viewModel.Cells[index]);
        }

        var puzzle = SudokuDefaults.SamplePuzzle;
        for (var i = 0; i < puzzle.Length; i++)
        {
            if (!viewModel.GivenCells[i])
            {
                continue;
            }

            Assert.Equal(puzzle[i] - '0', viewModel.Cells[i]);
        }
    }
}
