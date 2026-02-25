using SudokuArena.Application.Puzzles;

namespace SudokuArena.Infrastructure.Puzzles;

public sealed class LocalSeedPuzzleProvider : IPuzzleProvider
{
    private readonly JsonPuzzleProvider _innerProvider;

    public LocalSeedPuzzleProvider(string datasetPath)
    {
        _innerProvider = new JsonPuzzleProvider(datasetPath);
    }

    public PuzzleDefinition? GetNext(DifficultyTier difficultyTier) => _innerProvider.GetNext(difficultyTier);
}
