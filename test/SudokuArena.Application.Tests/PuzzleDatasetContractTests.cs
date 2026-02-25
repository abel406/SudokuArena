using System.Text.Json;
using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.Tests;

public sealed class PuzzleDatasetContractTests
{
    [Fact]
    public void PuzzleDatasetDocument_ShouldSerializeWithSchemaAndSnakeCaseFields()
    {
        var generatedAt = new DateTimeOffset(2026, 2, 25, 0, 0, 0, TimeSpan.Zero);
        var document = new PuzzleDatasetDocument(
            PuzzleDatasetSchema.Version1,
            generatedAt,
            [
                new PuzzleQuestionEntry(
                    "puz-0001",
                    "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
                    "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                    DifficultyTier.Medium,
                    30,
                    PuzzleBoardKind.Classic9x9,
                    PuzzleMode.Medium)
            ],
            new Dictionary<string, PuzzleSolverDetailEntry>
            {
                ["puz-0001"] = new PuzzleSolverDetailEntry(
                    2.7,
                    4,
                    1,
                    new Dictionary<string, int>
                    {
                        ["single"] = 38,
                        ["hidden_single"] = 11
                    })
            },
            new Dictionary<string, int[]>
            {
                ["puz-0001"] = [120, 180, 260, 360]
            });

        var json = JsonSerializer.Serialize(document);

        Assert.Contains("\"schema_version\":\"sudokuarena.puzzle_dataset.v1\"", json);
        Assert.Contains("\"question_bank\"", json);
        Assert.Contains("\"solver_details\"", json);
        Assert.Contains("\"time_map\"", json);
        Assert.Contains("\"board_kind\":\"Classic9x9\"", json);
        Assert.Contains("\"mode\":\"Medium\"", json);
        Assert.Contains("\"difficulty_tier\":\"Medium\"", json);
    }

    [Fact]
    public void PuzzleDatasetDocument_ShouldDeserializeBackToTypedModel()
    {
        const string json = """
{
  "schema_version": "sudokuarena.puzzle_dataset.v1",
  "generated_at_utc": "2026-02-25T00:00:00Z",
  "question_bank": [
    {
      "puzzle_id": "puz-0001",
      "board_kind": "Classic9x9",
      "mode": "Hard",
      "puzzle": "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
      "solution": "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
      "difficulty_tier": "Hard",
      "given_count": 30
    }
  ],
  "solver_details": {
    "puz-0001": {
      "weighted_se": 3.9,
      "max_rate": 5,
      "advanced_hits": 3,
      "technique_counts": {
        "single": 21,
        "hidden_single": 12
      }
    }
  },
  "time_map": {
    "puz-0001": [150, 220, 300, 420]
  }
}
""";

        var model = JsonSerializer.Deserialize<PuzzleDatasetDocument>(json);

        Assert.NotNull(model);
        Assert.Equal(PuzzleDatasetSchema.Version1, model!.SchemaVersion);
        Assert.Single(model.QuestionBank);
        Assert.Equal(DifficultyTier.Hard, model.QuestionBank[0].DifficultyTier);
        Assert.Equal(PuzzleBoardKind.Classic9x9, model.QuestionBank[0].BoardKind);
        Assert.Equal(PuzzleMode.Hard, model.QuestionBank[0].Mode);
        Assert.Equal(3.9, model.SolverDetails["puz-0001"].WeightedScoreEstimate);
        Assert.Equal([150, 220, 300, 420], model.TimeMap["puz-0001"]);
    }

    [Fact]
    public void PuzzleModeResolver_ShouldInferMode_WhenModeIsUnknown()
    {
        var nineByNine = PuzzleModeResolver.Resolve(PuzzleBoardKind.Classic9x9, DifficultyTier.Hard, PuzzleMode.Unknown);
        var sixBySix = PuzzleModeResolver.Resolve(PuzzleBoardKind.SixBySix, DifficultyTier.Beginner, PuzzleMode.Unknown);
        var sixteen = PuzzleModeResolver.Resolve(PuzzleBoardKind.SixteenBySixteen, DifficultyTier.Expert, PuzzleMode.Unknown);

        Assert.Equal(PuzzleMode.Hard, nineByNine);
        Assert.Equal(PuzzleMode.Six, sixBySix);
        Assert.Equal(PuzzleMode.Sixteen, sixteen);
    }

    [Fact]
    public void DifficultyTier_ShouldExposeStableFiveLevelScale()
    {
        var values = Enum.GetValues<DifficultyTier>();

        Assert.Equal(5, values.Length);
        Assert.Equal(DifficultyTier.Beginner, values[0]);
        Assert.Equal(DifficultyTier.Expert, values[^1]);
    }
}
