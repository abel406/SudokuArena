using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Contracts;

public sealed record CreateMatchRequest(
    string HostPlayer,
    string GuestPlayer,
    MatchTransport Transport,
    string Puzzle);
