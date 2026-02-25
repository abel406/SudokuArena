using System.Text.Json;
using SudokuArena.Application.Puzzles;
using SudokuArena.Infrastructure.Puzzles;

namespace SudokuArena.Desktop.Tests;

public sealed class JsonPuzzleProviderTests
{
    [Fact]
    public void GetNext_ShouldReturnPuzzle_ForRequestedTier_OrNearestFallback()
    {
        var path = WriteDataset(BuildValidDocument());
        var provider = new JsonPuzzleProvider(path);

        var mediumPuzzle = provider.GetNext(DifficultyTier.Medium);
        var hardPuzzle = provider.GetNext(DifficultyTier.Hard);
        var easyPuzzle = provider.GetNext(DifficultyTier.Easy);

        Assert.NotNull(mediumPuzzle);
        Assert.Equal("med-1", mediumPuzzle!.PuzzleId);
        Assert.NotNull(hardPuzzle);
        Assert.Equal("hrd-1", hardPuzzle!.PuzzleId);
        Assert.NotNull(easyPuzzle);
        Assert.Equal("med-1", easyPuzzle!.PuzzleId);
    }

    [Fact]
    public void GetNext_ShouldPreferLowerTier_WhenFallbackDistanceIsEqual()
    {
        var path = WriteDataset(BuildDocumentWithEasyAndHardOnly());
        var provider = new JsonPuzzleProvider(path);

        var mediumRequest = provider.GetNext(DifficultyTier.Medium);

        Assert.NotNull(mediumRequest);
        Assert.Equal("eas-1", mediumRequest!.PuzzleId);
        Assert.Equal(DifficultyTier.Easy, mediumRequest.DifficultyTier);
    }

    [Fact]
    public void GetNext_ShouldAvoidImmediateRepeat_WhenTierHasMultiplePuzzles()
    {
        var path = WriteDataset(BuildDocumentWithTwoMediumPuzzles());
        var provider = new JsonPuzzleProvider(path);

        var first = provider.GetNext(DifficultyTier.Medium);
        var second = provider.GetNext(DifficultyTier.Medium);
        var third = provider.GetNext(DifficultyTier.Medium);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.NotEqual(first!.PuzzleId, second!.PuzzleId);
        Assert.Equal(first.PuzzleId, third!.PuzzleId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSchemaVersionIsInvalid()
    {
        var invalid = BuildValidDocument() with
        {
            SchemaVersion = "unsupported.version"
        };
        var path = WriteDataset(invalid);

        Assert.Throws<FormatException>(() => new JsonPuzzleProvider(path));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSolverDetailsAreMissing()
    {
        var valid = BuildValidDocument();
        var invalid = valid with
        {
            SolverDetails = new Dictionary<string, PuzzleSolverDetailEntry>()
        };
        var path = WriteDataset(invalid);

        Assert.Throws<FormatException>(() => new JsonPuzzleProvider(path));
    }

    [Fact]
    public void GetNext_ShouldIgnoreUnsupportedBoardKinds_ForCurrentDesktopRuntime()
    {
        var document = new PuzzleDatasetDocument(
            PuzzleDatasetSchema.Version1,
            new DateTimeOffset(2026, 2, 25, 0, 0, 0, TimeSpan.Zero),
            [
                new PuzzleQuestionEntry(
                    "six-1",
                    "123456123456123456123456123456123456",
                    "123456123456123456123456123456123456",
                    DifficultyTier.Beginner,
                    36,
                    PuzzleBoardKind.SixBySix,
                    PuzzleMode.Six),
                new PuzzleQuestionEntry(
                    "med-1",
                    ".3.6.8.1.6.2.9.3.8.9.3.2.6.8.9.6.4.3.2.8.3.9.7.3.2.8.6.6.5.7.8.2.7.1.6.5.4.2.6.7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Medium,
                    40)
            ],
            new Dictionary<string, PuzzleSolverDetailEntry>
            {
                ["med-1"] = new PuzzleSolverDetailEntry(2.8, 3, 1, new Dictionary<string, int> { ["single"] = 20 })
            },
            new Dictionary<string, int[]>
            {
                ["med-1"] = [150, 210, 290, 390]
            });

        var path = WriteDataset(document);
        var provider = new JsonPuzzleProvider(path);

        var puzzle = provider.GetNext(DifficultyTier.Medium);

        Assert.NotNull(puzzle);
        Assert.Equal("med-1", puzzle!.PuzzleId);
        Assert.Equal(PuzzleBoardKind.Classic9x9, puzzle.BoardKind);
    }

    private static PuzzleDatasetDocument BuildValidDocument()
    {
        return new PuzzleDatasetDocument(
            PuzzleDatasetSchema.Version1,
            new DateTimeOffset(2026, 2, 25, 0, 0, 0, TimeSpan.Zero),
            [
                new PuzzleQuestionEntry(
                    "med-1",
                    ".3.6.8.1.6.2.9.3.8.9.3.2.6.8.9.6.4.3.2.8.3.9.7.3.2.8.6.6.5.7.8.2.7.1.6.5.4.2.6.7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Medium,
                    40),
                new PuzzleQuestionEntry(
                    "hrd-1",
                    ".3.6.8...6.2.9.3.8.9...2.6.8.9.6.4...2.8.3.9.7.3...8.6.6.5.7.8...7.1.6.5.4.2...7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Hard,
                    34)
            ],
            new Dictionary<string, PuzzleSolverDetailEntry>
            {
                ["med-1"] = new PuzzleSolverDetailEntry(2.8, 3, 1, new Dictionary<string, int> { ["single"] = 20 }),
                ["hrd-1"] = new PuzzleSolverDetailEntry(3.7, 4, 3, new Dictionary<string, int> { ["single"] = 15 })
            },
            new Dictionary<string, int[]>
            {
                ["med-1"] = [150, 210, 290, 390],
                ["hrd-1"] = [190, 260, 360, 500]
            });
    }

    private static PuzzleDatasetDocument BuildDocumentWithEasyAndHardOnly()
    {
        return new PuzzleDatasetDocument(
            PuzzleDatasetSchema.Version1,
            new DateTimeOffset(2026, 2, 25, 0, 0, 0, TimeSpan.Zero),
            [
                new PuzzleQuestionEntry(
                    "eas-1",
                    ".34.78.12.72.95.48.98.42.67.59.61.23.26.53.91.13.24.56.61.37.84.87.19.35.45.86.79",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Easy,
                    54),
                new PuzzleQuestionEntry(
                    "hrd-1",
                    ".3.6.8...6.2.9.3.8.9...2.6.8.9.6.4...2.8.3.9.7.3...8.6.6.5.7.8...7.1.6.5.4.2...7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Hard,
                    34)
            ],
            new Dictionary<string, PuzzleSolverDetailEntry>
            {
                ["eas-1"] = new PuzzleSolverDetailEntry(1.9, 2, 0, new Dictionary<string, int> { ["single"] = 40 }),
                ["hrd-1"] = new PuzzleSolverDetailEntry(3.7, 4, 3, new Dictionary<string, int> { ["single"] = 15 })
            },
            new Dictionary<string, int[]>
            {
                ["eas-1"] = [120, 160, 230, 320],
                ["hrd-1"] = [190, 260, 360, 500]
            });
    }

    private static PuzzleDatasetDocument BuildDocumentWithTwoMediumPuzzles()
    {
        return new PuzzleDatasetDocument(
            PuzzleDatasetSchema.Version1,
            new DateTimeOffset(2026, 2, 25, 0, 0, 0, TimeSpan.Zero),
            [
                new PuzzleQuestionEntry(
                    "med-1",
                    ".3.6.8.1.6.2.9.3.8.9.3.2.6.8.9.6.4.3.2.8.3.9.7.3.2.8.6.6.5.7.8.2.7.1.6.5.4.2.6.7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Medium,
                    40),
                new PuzzleQuestionEntry(
                    "med-2",
                    ".3.6.8.1.6.2.9.3.8.9.3.2.6.8.9.6.4.3.2.8.3.9.7.3.2.8.6.6.5.7.8.2.7.1.6.5.4.2.6.7.",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Medium,
                    40)
            ],
            new Dictionary<string, PuzzleSolverDetailEntry>
            {
                ["med-1"] = new PuzzleSolverDetailEntry(2.8, 3, 1, new Dictionary<string, int> { ["single"] = 20 }),
                ["med-2"] = new PuzzleSolverDetailEntry(2.9, 3, 1, new Dictionary<string, int> { ["single"] = 21 })
            },
            new Dictionary<string, int[]>
            {
                ["med-1"] = [150, 210, 290, 390],
                ["med-2"] = [155, 215, 300, 405]
            });
    }

    private static string WriteDataset(PuzzleDatasetDocument document)
    {
        var path = Path.Combine(Path.GetTempPath(), "SudokuArenaTests", Guid.NewGuid().ToString("N"), "dataset.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(document));
        return path;
    }
}
