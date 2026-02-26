using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.AutoComplete;

public interface IAutoCompletePolicyEvaluator
{
    AutoCompletePolicyDecision Evaluate(AutoCompletePolicyInput input);
}

public sealed record AutoCompletePolicyInput(
    bool AutoCompleteEnabled,
    bool IsGameFinished,
    bool IsAwaitingDefeatDecision,
    bool IsSessionCancelledForMatch,
    int RemainingEditableToSolve,
    DifficultyTier DifficultyTier);

public sealed record AutoCompletePolicyDecision(
    bool ShouldPrompt,
    int TickIntervalMilliseconds,
    int MinRemainingToTrigger,
    int MaxRemainingToTrigger);
