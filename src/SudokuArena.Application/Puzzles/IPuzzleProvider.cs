namespace SudokuArena.Application.Puzzles;

public interface IPuzzleProvider
{
    PuzzleDefinition? GetNext(DifficultyTier difficultyTier);
}
