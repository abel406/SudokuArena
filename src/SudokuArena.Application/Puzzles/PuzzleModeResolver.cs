namespace SudokuArena.Application.Puzzles;

public static class PuzzleModeResolver
{
    public static PuzzleMode Resolve(PuzzleBoardKind boardKind, DifficultyTier tier, PuzzleMode requestedMode)
    {
        if (requestedMode is not PuzzleMode.Unknown)
        {
            return requestedMode;
        }

        return boardKind switch
        {
            PuzzleBoardKind.SixBySix => PuzzleMode.Six,
            PuzzleBoardKind.SixteenBySixteen => PuzzleMode.Sixteen,
            _ => tier switch
            {
                DifficultyTier.Beginner => PuzzleMode.Beginner,
                DifficultyTier.Easy => PuzzleMode.Easy,
                DifficultyTier.Medium => PuzzleMode.Medium,
                DifficultyTier.Hard => PuzzleMode.Hard,
                _ => PuzzleMode.Expert
            }
        };
    }
}
