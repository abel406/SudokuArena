namespace SudokuArena.Desktop.Theming;

public sealed record AutoCompleteTelemetrySnapshot(
    int Starts,
    int Cancellations,
    int FilledCells);
