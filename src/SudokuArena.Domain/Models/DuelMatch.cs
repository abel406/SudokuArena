namespace SudokuArena.Domain.Models;

public sealed class DuelMatch
{
    public DuelMatch(
        Guid id,
        string hostPlayer,
        string guestPlayer,
        MatchTransport transport,
        string initialPuzzle,
        SudokuBoard board,
        DateTimeOffset createdUtc,
        DateTimeOffset? completedUtc = null)
    {
        Id = id;
        HostPlayer = hostPlayer;
        GuestPlayer = guestPlayer;
        Transport = transport;
        InitialPuzzle = initialPuzzle;
        Board = board;
        CreatedUtc = createdUtc;
        CompletedUtc = completedUtc;
    }

    public Guid Id { get; }

    public string HostPlayer { get; }

    public string GuestPlayer { get; }

    public MatchTransport Transport { get; }

    public string InitialPuzzle { get; }

    public SudokuBoard Board { get; }

    public DateTimeOffset CreatedUtc { get; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    public bool IsCompleted => CompletedUtc is not null;

    public MoveResult RegisterMove(string playerEmail, int index, int? value, DateTimeOffset playedUtc)
    {
        if (IsCompleted)
        {
            return MoveResult.Rejected("Match is already completed.");
        }

        if (!IsPlayerInMatch(playerEmail))
        {
            return MoveResult.Rejected("Player is not part of this match.");
        }

        if (!Board.TrySetCell(index, value, out var reason))
        {
            return MoveResult.Rejected(reason ?? "Invalid move.");
        }

        if (Board.IsComplete)
        {
            CompletedUtc = playedUtc;
        }

        return MoveResult.Success(Board.IsComplete);
    }

    public bool IsPlayerInMatch(string playerEmail)
    {
        return string.Equals(HostPlayer, playerEmail, StringComparison.OrdinalIgnoreCase)
               || string.Equals(GuestPlayer, playerEmail, StringComparison.OrdinalIgnoreCase);
    }
}
