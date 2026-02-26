using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.AutoComplete;

public sealed class AutoCompletePolicyEvaluator : IAutoCompletePolicyEvaluator
{
    private static readonly IReadOnlyDictionary<DifficultyTier, AutoCompleteTierCalibration> TierCalibrations =
        new Dictionary<DifficultyTier, AutoCompleteTierCalibration>
        {
            [DifficultyTier.Beginner] = new(6, 10, 300),
            [DifficultyTier.Easy] = new(5, 9, 275),
            [DifficultyTier.Medium] = new(5, 9, 250),
            [DifficultyTier.Hard] = new(4, 8, 225),
            [DifficultyTier.Expert] = new(4, 7, 200)
        };

    public AutoCompletePolicyDecision Evaluate(AutoCompletePolicyInput input)
    {
        var calibration = TierCalibrations.TryGetValue(input.DifficultyTier, out var byTier)
            ? byTier
            : TierCalibrations[DifficultyTier.Medium];

        var remaining = Math.Max(0, input.RemainingEditableToSolve);
        var shouldPrompt = input.AutoCompleteEnabled &&
                           !input.IsGameFinished &&
                           !input.IsAwaitingDefeatDecision &&
                           !input.IsSessionCancelledForMatch &&
                           remaining >= calibration.MinRemainingToTrigger &&
                           remaining <= calibration.MaxRemainingToTrigger;

        return new AutoCompletePolicyDecision(
            shouldPrompt,
            calibration.TickIntervalMilliseconds,
            calibration.MinRemainingToTrigger,
            calibration.MaxRemainingToTrigger);
    }

    private sealed record AutoCompleteTierCalibration(
        int MinRemainingToTrigger,
        int MaxRemainingToTrigger,
        int TickIntervalMilliseconds);
}
