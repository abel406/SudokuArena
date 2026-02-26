using SudokuArena.Application.AutoComplete;
using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.Tests;

public sealed class AutoCompletePolicyEvaluatorTests
{
    private readonly AutoCompletePolicyEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ShouldPromptForMedium_WhenRemainingIsWithinRange()
    {
        var decision = _evaluator.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled: true,
            IsGameFinished: false,
            IsAwaitingDefeatDecision: false,
            IsSessionCancelledForMatch: false,
            RemainingEditableToSolve: 9,
            DifficultyTier: DifficultyTier.Medium));

        Assert.True(decision.ShouldPrompt);
        Assert.Equal(5, decision.MinRemainingToTrigger);
        Assert.Equal(9, decision.MaxRemainingToTrigger);
        Assert.Equal(250, decision.TickIntervalMilliseconds);
    }

    [Fact]
    public void Evaluate_ShouldNotPrompt_WhenRemainingIsOutOfRange()
    {
        var decision = _evaluator.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled: true,
            IsGameFinished: false,
            IsAwaitingDefeatDecision: false,
            IsSessionCancelledForMatch: false,
            RemainingEditableToSolve: 4,
            DifficultyTier: DifficultyTier.Medium));

        Assert.False(decision.ShouldPrompt);
    }

    [Fact]
    public void Evaluate_ShouldApplyDifficultyCalibration_ForExpert()
    {
        var decision = _evaluator.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled: true,
            IsGameFinished: false,
            IsAwaitingDefeatDecision: false,
            IsSessionCancelledForMatch: false,
            RemainingEditableToSolve: 7,
            DifficultyTier: DifficultyTier.Expert));

        Assert.True(decision.ShouldPrompt);
        Assert.Equal(4, decision.MinRemainingToTrigger);
        Assert.Equal(7, decision.MaxRemainingToTrigger);
        Assert.Equal(200, decision.TickIntervalMilliseconds);
    }

    [Fact]
    public void Evaluate_ShouldBlockPrompt_WhenSessionCancelledForMatch()
    {
        var decision = _evaluator.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled: true,
            IsGameFinished: false,
            IsAwaitingDefeatDecision: false,
            IsSessionCancelledForMatch: true,
            RemainingEditableToSolve: 7,
            DifficultyTier: DifficultyTier.Easy));

        Assert.False(decision.ShouldPrompt);
    }

    [Fact]
    public void Evaluate_ShouldUseExternalCalibration_WhenAvailable()
    {
        var evaluator = new AutoCompletePolicyEvaluator(new FakeCalibrationSource());
        var decision = evaluator.Evaluate(new AutoCompletePolicyInput(
            AutoCompleteEnabled: true,
            IsGameFinished: false,
            IsAwaitingDefeatDecision: false,
            IsSessionCancelledForMatch: false,
            RemainingEditableToSolve: 12,
            DifficultyTier: DifficultyTier.Medium));

        Assert.True(decision.ShouldPrompt);
        Assert.Equal(10, decision.MinRemainingToTrigger);
        Assert.Equal(13, decision.MaxRemainingToTrigger);
        Assert.Equal(320, decision.TickIntervalMilliseconds);
    }

    private sealed class FakeCalibrationSource : IAutoCompleteCalibrationSource
    {
        public AutoCompleteTierCalibration? GetCalibration(DifficultyTier difficultyTier)
        {
            return difficultyTier == DifficultyTier.Medium
                ? new AutoCompleteTierCalibration(10, 13, 320)
                : null;
        }
    }
}
