using SudokuArena.Application.Scoring;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelScoringTests
{
    private const string SolvedBoard =
        "534678912" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286179";

    private const string PuzzleWithSingleGap =
        "534678910" +
        "672195348" +
        "198342567" +
        "859761423" +
        "426853791" +
        "713924856" +
        "961537284" +
        "287419635" +
        "345286179";

    [Fact]
    public void ApplyCellEdit_ShouldBuildVictorySummary_WhenPuzzleIsCompleted()
    {
        var viewModel = new MainViewModel(PuzzleWithSingleGap, SolvedBoard);

        viewModel.ApplyCellEdit(8, 2, saveHistory: true);

        Assert.True(viewModel.IsGameFinished);
        Assert.True(viewModel.IsVictory);
        Assert.NotNull(viewModel.LastVictorySummary);
        Assert.Equal(viewModel.Score, viewModel.LastVictorySummary!.FinalScore);
        Assert.Equal(viewModel.OldScore, viewModel.LastVictorySummary.OldScore);
        Assert.Equal(viewModel.NewScore, viewModel.LastVictorySummary.NewScore);
        Assert.Equal(ScoreVersion.Old, viewModel.LastVictorySummary.ScoreVersion);
        Assert.Equal(viewModel.DifficultyLabel, viewModel.LastVictorySummary.DifficultyLabel);
        Assert.Equal(0, viewModel.LastVictorySummary.ErrorCount);
        Assert.True(viewModel.LastVictorySummary.IsPerfect);
        Assert.Contains(':', viewModel.LastVictorySummary.ElapsedTimeText);
    }

    [Fact]
    public void ApplyCellEdit_ShouldMarkNonPerfectVictory_WhenHadInvalidMoves()
    {
        var viewModel = new MainViewModel(PuzzleWithSingleGap, SolvedBoard);

        viewModel.ApplyCellEdit(8, 1, saveHistory: true);
        viewModel.ApplyCellEdit(8, 2, saveHistory: true);

        Assert.NotNull(viewModel.LastVictorySummary);
        Assert.Equal(1, viewModel.LastVictorySummary!.ErrorCount);
        Assert.False(viewModel.LastVictorySummary.IsPerfect);
        Assert.Equal(0, viewModel.LastVictorySummary.PerfectBonusScore);
        Assert.True(viewModel.LastVictorySummary.FinalScore > 0);
    }
}
