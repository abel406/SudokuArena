using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelDifficultyTierTests
{
    [Fact]
    public void Constructor_ShouldDefaultToMediumDifficultyTier()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(DifficultyTier.Medium, viewModel.SelectedDifficultyTier);
        Assert.Equal("Medio", viewModel.DifficultyLabel);
    }

    [Fact]
    public void DifficultyTierOptions_ShouldExposeAllSupportedValues()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(Enum.GetValues<DifficultyTier>(), viewModel.DifficultyTierOptions);
    }

    [Fact]
    public void ChangingSelectedDifficultyTier_ShouldUpdateDifficultyLabel()
    {
        var viewModel = new MainViewModel();

        viewModel.SelectedDifficultyTier = DifficultyTier.Hard;

        Assert.Equal("Dificil", viewModel.DifficultyLabel);
    }
}
