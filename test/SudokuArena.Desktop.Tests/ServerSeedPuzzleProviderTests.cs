using SudokuArena.Application.Puzzles;
using SudokuArena.Infrastructure.Puzzles;

namespace SudokuArena.Desktop.Tests;

public sealed class ServerSeedPuzzleProviderTests
{
    [Fact]
    public void Constructor_ShouldAllowMissingDataset_WhenNotRequired()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "SudokuArenaTests", Guid.NewGuid().ToString("N"), "missing.json");

        var provider = new ServerSeedPuzzleProvider(missingPath, required: false);

        var result = provider.GetNext(DifficultyTier.Medium);
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDatasetIsMissing_AndRequired()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "SudokuArenaTests", Guid.NewGuid().ToString("N"), "missing.json");

        Assert.Throws<FileNotFoundException>(() => new ServerSeedPuzzleProvider(missingPath, required: true));
    }
}
