using SudokuArena.Application.Puzzles;

namespace SudokuArena.Infrastructure.Puzzles;

public sealed class ServerSeedPuzzleProvider : IPuzzleProvider
{
    private readonly JsonPuzzleProvider? _innerProvider;

    public ServerSeedPuzzleProvider(string datasetPath, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(datasetPath))
        {
            throw new ArgumentException("Dataset path cannot be empty.", nameof(datasetPath));
        }

        if (!File.Exists(datasetPath))
        {
            if (required)
            {
                throw new FileNotFoundException("Server seed dataset file was not found.", datasetPath);
            }

            return;
        }

        _innerProvider = new JsonPuzzleProvider(datasetPath);
    }

    public PuzzleDefinition? GetNext(DifficultyTier difficultyTier) => _innerProvider?.GetNext(difficultyTier);
}
