using SudokuArena.Domain.Models;

namespace SudokuArena.Domain.Tests;

public sealed class SudokuBoardTests
{
    [Fact]
    public void CreateFromString_ShouldMarkGivenCells()
    {
        var board = SudokuBoard.CreateFromString(SudokuDefaults.SamplePuzzle);

        Assert.True(board.Givens[0]);
        Assert.False(board.Givens[2]);
    }

    [Fact]
    public void TrySetCell_ShouldRejectConflict()
    {
        var board = SudokuBoard.CreateFromString(SudokuDefaults.SamplePuzzle);

        var success = board.TrySetCell(2, 5, out var reason);

        Assert.False(success);
        Assert.NotNull(reason);
    }

    [Fact]
    public void TrySetCell_ShouldAllowValidNumber()
    {
        var board = SudokuBoard.CreateFromString(SudokuDefaults.SamplePuzzle);

        var success = board.TrySetCell(2, 4, out var reason);

        Assert.True(success);
        Assert.Null(reason);
    }
}
