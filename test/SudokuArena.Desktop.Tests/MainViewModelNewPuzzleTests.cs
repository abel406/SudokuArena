using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Tests;

public sealed class MainViewModelNewPuzzleTests
{
    [Fact]
    public void NewPuzzleCommand_ShouldRequestSelectedTier_AndLoadReturnedPuzzle()
    {
        var fakeProvider = new FakePuzzleProvider();
        var viewModel = new MainViewModel(fakeProvider)
        {
            SelectedDifficultyTier = DifficultyTier.Hard
        };

        viewModel.NewPuzzleCommand.Execute(null);

        Assert.Equal([DifficultyTier.Hard], fakeProvider.RequestedTiers);
        Assert.Null(viewModel.Cells[0]);
        Assert.Equal(3, viewModel.Cells[1]);
        Assert.Equal("Dificil", viewModel.DifficultyLabel);
        Assert.Equal("Nuevo puzzle cargado (Dificil).", viewModel.StatusMessage);
    }

    [Fact]
    public void NewPuzzleCommand_ShouldShowMessage_WhenNoPuzzleIsAvailableForTier()
    {
        var fakeProvider = new FakePuzzleProvider(returnNull: true);
        var viewModel = new MainViewModel(fakeProvider)
        {
            SelectedDifficultyTier = DifficultyTier.Expert
        };

        var originalCell0 = viewModel.Cells[0];
        viewModel.NewPuzzleCommand.Execute(null);

        Assert.Equal([DifficultyTier.Expert], fakeProvider.RequestedTiers);
        Assert.Equal(originalCell0, viewModel.Cells[0]);
        Assert.Equal("No hay puzzles disponibles para Experto.", viewModel.StatusMessage);
    }

    private sealed class FakePuzzleProvider(bool returnNull = false) : IPuzzleProvider
    {
        public List<DifficultyTier> RequestedTiers { get; } = [];

        public PuzzleDefinition? GetNext(DifficultyTier difficultyTier)
        {
            RequestedTiers.Add(difficultyTier);
            if (returnNull)
            {
                return null;
            }

            return new PuzzleDefinition(
                "hrd-1",
                ".3.6.8...6.2.9.3.8.9...2.6.8.9.6.4...2.8.3.9.7.3...8.6.6.5.7.8...7.1.6.5.4.2...7.",
                "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                difficultyTier,
                34,
                3.7,
                4,
                3);
        }
    }
}
