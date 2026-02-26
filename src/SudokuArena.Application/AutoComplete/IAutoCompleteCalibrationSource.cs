using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.AutoComplete;

public interface IAutoCompleteCalibrationSource
{
    AutoCompleteTierCalibration? GetCalibration(DifficultyTier difficultyTier);
}

public sealed record AutoCompleteTierCalibration(
    int MinRemainingToTrigger,
    int MaxRemainingToTrigger,
    int TickIntervalMilliseconds);
