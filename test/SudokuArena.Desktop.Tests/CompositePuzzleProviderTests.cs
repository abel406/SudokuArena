using SudokuArena.Application.Puzzles;
using SudokuArena.Infrastructure.Puzzles;

namespace SudokuArena.Desktop.Tests;

public sealed class CompositePuzzleProviderTests
{
    [Fact]
    public void GetNext_ShouldUseFirstProvider_WhenItReturnsPuzzle()
    {
        var expected = BuildPuzzle("srv-0001", DifficultyTier.Hard);
        var first = new FakePuzzleProvider(expected);
        var second = new FakePuzzleProvider(BuildPuzzle("loc-0001", DifficultyTier.Hard));
        var composite = new CompositePuzzleProvider(first, second);

        var result = composite.GetNext(DifficultyTier.Hard);

        Assert.NotNull(result);
        Assert.Equal("srv-0001", result!.PuzzleId);
        Assert.Equal(1, first.RequestCount);
        Assert.Equal(0, second.RequestCount);
    }

    [Fact]
    public void GetNext_ShouldFallbackToSecondProvider_WhenFirstReturnsNull()
    {
        var first = new FakePuzzleProvider(null);
        var second = new FakePuzzleProvider(BuildPuzzle("loc-0001", DifficultyTier.Medium));
        var composite = new CompositePuzzleProvider(first, second);

        var result = composite.GetNext(DifficultyTier.Medium);

        Assert.NotNull(result);
        Assert.Equal("loc-0001", result!.PuzzleId);
        Assert.Equal(1, first.RequestCount);
        Assert.Equal(1, second.RequestCount);
    }

    [Fact]
    public void GetNext_ShouldReturnNull_WhenAllProvidersReturnNull()
    {
        var composite = new CompositePuzzleProvider(
            new FakePuzzleProvider(null),
            new FakePuzzleProvider(null));

        var result = composite.GetNext(DifficultyTier.Expert);

        Assert.Null(result);
    }

    [Fact]
    public void GetNext_ShouldAvoidImmediateRepeat_AcrossSources_WhenAlternativeExists()
    {
        var repeated = BuildPuzzle("shared-0001", DifficultyTier.Medium);
        var alternative = BuildPuzzle("shared-0002", DifficultyTier.Medium);

        var first = new QueuePuzzleProvider([repeated, repeated]);
        var second = new QueuePuzzleProvider([alternative, alternative]);
        var composite = new CompositePuzzleProvider(first, second);

        var firstPick = composite.GetNext(DifficultyTier.Medium);
        var secondPick = composite.GetNext(DifficultyTier.Medium);

        Assert.NotNull(firstPick);
        Assert.NotNull(secondPick);
        Assert.Equal("shared-0001", firstPick!.PuzzleId);
        Assert.Equal("shared-0002", secondPick!.PuzzleId);
    }

    private static PuzzleDefinition BuildPuzzle(string id, DifficultyTier tier)
    {
        const string puzzle = "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79";
        const string solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179";
        return new PuzzleDefinition(id, puzzle, solution, tier, 30, 2.5, 4, 1);
    }

    private sealed class FakePuzzleProvider(PuzzleDefinition? nextPuzzle) : IPuzzleProvider
    {
        private readonly PuzzleDefinition? _nextPuzzle = nextPuzzle;

        public int RequestCount { get; private set; }

        public PuzzleDefinition? GetNext(DifficultyTier difficultyTier)
        {
            RequestCount++;
            return _nextPuzzle;
        }
    }

    private sealed class QueuePuzzleProvider(IReadOnlyList<PuzzleDefinition?> sequence) : IPuzzleProvider
    {
        private int _index;

        public PuzzleDefinition? GetNext(DifficultyTier difficultyTier)
        {
            if (sequence.Count == 0)
            {
                return null;
            }

            if (_index >= sequence.Count)
            {
                return sequence[^1];
            }

            var next = sequence[_index];
            _index++;
            return next;
        }
    }
}
