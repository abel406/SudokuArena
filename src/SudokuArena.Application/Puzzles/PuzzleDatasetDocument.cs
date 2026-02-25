using System.Text.Json.Serialization;

namespace SudokuArena.Application.Puzzles;

public static class PuzzleDatasetSchema
{
    public const string Version1 = "sudokuarena.puzzle_dataset.v1";
}

public sealed record PuzzleDatasetDocument(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("generated_at_utc")] DateTimeOffset GeneratedAtUtc,
    [property: JsonPropertyName("question_bank")] IReadOnlyList<PuzzleQuestionEntry> QuestionBank,
    [property: JsonPropertyName("solver_details")] IReadOnlyDictionary<string, PuzzleSolverDetailEntry> SolverDetails,
    [property: JsonPropertyName("time_map")] IReadOnlyDictionary<string, int[]> TimeMap);

public sealed record PuzzleQuestionEntry(
    [property: JsonPropertyName("puzzle_id")] string PuzzleId,
    [property: JsonPropertyName("puzzle")] string Puzzle,
    [property: JsonPropertyName("solution")] string Solution,
    [property: JsonPropertyName("difficulty_tier")] DifficultyTier DifficultyTier,
    [property: JsonPropertyName("given_count")] int GivenCount);

public sealed record PuzzleSolverDetailEntry(
    [property: JsonPropertyName("weighted_se")] double WeightedScoreEstimate,
    [property: JsonPropertyName("max_rate")] int MaxTechniqueRate,
    [property: JsonPropertyName("advanced_hits")] int AdvancedHits,
    [property: JsonPropertyName("technique_counts")] IReadOnlyDictionary<string, int> TechniqueCounts);
