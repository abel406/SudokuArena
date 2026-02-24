using System.Text.Json;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Contracts;
using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Services;

public sealed class MatchService(IMatchRepository repository, IOutboxRepository outboxRepository) : IMatchService
{
    public async Task<MatchSnapshot> CreateAsync(CreateMatchRequest request, CancellationToken cancellationToken)
    {
        var match = new DuelMatch(
            Guid.NewGuid(),
            request.HostPlayer,
            request.GuestPlayer,
            request.Transport,
            request.Puzzle,
            SudokuBoard.CreateFromString(request.Puzzle),
            DateTimeOffset.UtcNow);

        await repository.SaveAsync(match, cancellationToken);
        await outboxRepository.EnqueueAsync(
            "match.created",
            JsonSerializer.Serialize(ToSnapshot(match)),
            cancellationToken);

        return ToSnapshot(match);
    }

    public async Task<MatchSnapshot?> GetAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await repository.GetByIdAsync(matchId, cancellationToken);
        return match is null ? null : ToSnapshot(match);
    }

    public async Task<MoveResponse> SubmitMoveAsync(MoveSubmission submission, CancellationToken cancellationToken)
    {
        var match = await repository.GetByIdAsync(submission.MatchId, cancellationToken);
        if (match is null)
        {
            return new MoveResponse(false, false, "Match not found.", string.Empty);
        }

        var moveResult = match.RegisterMove(
            submission.PlayerEmail,
            submission.CellIndex,
            submission.Value,
            DateTimeOffset.UtcNow);

        if (!moveResult.Accepted)
        {
            return new MoveResponse(false, false, moveResult.Error, match.Board.ToStateString());
        }

        await repository.SaveAsync(match, cancellationToken);
        await outboxRepository.EnqueueAsync(
            "match.move",
            JsonSerializer.Serialize(new
            {
                submission.MatchId,
                submission.PlayerEmail,
                submission.CellIndex,
                submission.Value,
                moveResult.Completed
            }),
            cancellationToken);

        return new MoveResponse(true, moveResult.Completed, null, match.Board.ToStateString());
    }

    private static MatchSnapshot ToSnapshot(DuelMatch match)
    {
        return new MatchSnapshot(
            match.Id,
            match.HostPlayer,
            match.GuestPlayer,
            match.Transport,
            match.InitialPuzzle,
            match.Board.ToStateString(),
            match.IsCompleted,
            match.CreatedUtc,
            match.CompletedUtc);
    }
}
