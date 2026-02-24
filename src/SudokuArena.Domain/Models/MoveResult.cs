namespace SudokuArena.Domain.Models;

public sealed record MoveResult(bool Accepted, bool Completed, string? Error)
{
    public static MoveResult Rejected(string error) => new(false, false, error);

    public static MoveResult Success(bool completed) => new(true, completed, null);
}
