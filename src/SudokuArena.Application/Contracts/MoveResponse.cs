namespace SudokuArena.Application.Contracts;

public sealed record MoveResponse(bool Accepted, bool Completed, string? Error, string BoardState);
