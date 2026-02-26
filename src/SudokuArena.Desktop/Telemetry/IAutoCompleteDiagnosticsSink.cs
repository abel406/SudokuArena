namespace SudokuArena.Desktop.Telemetry;

public interface IAutoCompleteDiagnosticsSink
{
    void Record(AutoCompleteDiagnosticEvent diagnosticEvent);
}

public sealed record AutoCompleteDiagnosticEvent(
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string DifficultyTier,
    int RemainingToSolve,
    int QueueCompleted,
    int QueueTotal,
    int TickIntervalMilliseconds);
