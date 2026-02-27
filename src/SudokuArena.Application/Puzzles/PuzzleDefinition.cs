namespace SudokuArena.Application.Puzzles;

public sealed record PuzzleDefinition(
    string PuzzleId,
    string Puzzle,
    string Solution,
    DifficultyTier DifficultyTier,
    int GivenCount,
    double WeightedScoreEstimate,
    int MaxTechniqueRate,
    int AdvancedHits,
    PuzzleBoardKind BoardKind = PuzzleBoardKind.Classic9x9,
    PuzzleMode Mode = PuzzleMode.Unknown,
    IReadOnlyList<int>? TimeThresholds = null);
