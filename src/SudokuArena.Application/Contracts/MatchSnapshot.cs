using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Contracts;

public sealed record MatchSnapshot(
    Guid MatchId,
    string HostPlayer,
    string GuestPlayer,
    MatchTransport Transport,
    string InitialPuzzle,
    string BoardState,
    bool IsCompleted,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc);
