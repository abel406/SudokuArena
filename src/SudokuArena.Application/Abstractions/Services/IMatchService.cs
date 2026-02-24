using SudokuArena.Application.Contracts;

namespace SudokuArena.Application.Abstractions.Services;

public interface IMatchService
{
    Task<MatchSnapshot> CreateAsync(CreateMatchRequest request, CancellationToken cancellationToken);

    Task<MatchSnapshot?> GetAsync(Guid matchId, CancellationToken cancellationToken);

    Task<MoveResponse> SubmitMoveAsync(MoveSubmission submission, CancellationToken cancellationToken);
}
