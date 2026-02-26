using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.AutoComplete;

public sealed class AutoCompletePolicyEvaluator : IAutoCompletePolicyEvaluator
{
    private readonly IAutoCompleteCalibrationSource? _calibrationSource;

    public AutoCompletePolicyEvaluator(IAutoCompleteCalibrationSource? calibrationSource = null)
    {
        _calibrationSource = calibrationSource;
    }

    public AutoCompletePolicyDecision Evaluate(AutoCompletePolicyInput input)
    {
        var calibration = _calibrationSource?.GetCalibration(input.DifficultyTier) ??
                          AutoCompletePolicyDefaults.GetCalibration(input.DifficultyTier);

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
}
