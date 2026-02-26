using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.AutoComplete;

public static class AutoCompletePolicyDefaults
{
    private static readonly IReadOnlyDictionary<DifficultyTier, AutoCompleteTierCalibration> DefaultCalibrations =
        new Dictionary<DifficultyTier, AutoCompleteTierCalibration>
        {
            [DifficultyTier.Beginner] = new(6, 10, 300),
            [DifficultyTier.Easy] = new(5, 9, 275),
            [DifficultyTier.Medium] = new(5, 9, 250),
            [DifficultyTier.Hard] = new(4, 8, 225),
            [DifficultyTier.Expert] = new(4, 7, 200)
        };

    public static AutoCompleteTierCalibration GetCalibration(DifficultyTier difficultyTier)
    {
        return DefaultCalibrations.TryGetValue(difficultyTier, out var calibration)
            ? calibration
            : DefaultCalibrations[DifficultyTier.Medium];
    }
}
