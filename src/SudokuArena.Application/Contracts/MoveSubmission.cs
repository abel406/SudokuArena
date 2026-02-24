namespace SudokuArena.Application.Contracts;

public sealed record MoveSubmission(
    Guid MatchId,
    string PlayerEmail,
    int CellIndex,
    int? Value);
