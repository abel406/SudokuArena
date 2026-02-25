using SudokuArena.Application.Puzzles;

namespace SudokuArena.Infrastructure.Puzzles;

public sealed class CompositePuzzleProvider : IPuzzleProvider
{
    private readonly IReadOnlyList<IPuzzleProvider> _providers;
    private readonly object _sync = new();
    private string? _lastPuzzleId;

    public CompositePuzzleProvider(params IPuzzleProvider[] providers)
    {
        if (providers is null || providers.Length == 0)
        {
            throw new ArgumentException("At least one puzzle provider is required.", nameof(providers));
        }

        _providers = providers.ToList();
    }

    public PuzzleDefinition? GetNext(DifficultyTier difficultyTier)
    {
        lock (_sync)
        {
            PuzzleDefinition? repeatedCandidate = null;
            foreach (var provider in _providers)
            {
                var next = provider.GetNext(difficultyTier);
                if (next is null)
                {
                    continue;
                }

                if (!string.Equals(next.PuzzleId, _lastPuzzleId, StringComparison.Ordinal))
                {
                    _lastPuzzleId = next.PuzzleId;
                    return next;
                }

                repeatedCandidate ??= next;
            }

            if (repeatedCandidate is not null)
            {
                _lastPuzzleId = repeatedCandidate.PuzzleId;
                return repeatedCandidate;
            }

            return null;
        }
    }
}
