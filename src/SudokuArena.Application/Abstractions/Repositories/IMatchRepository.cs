using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Abstractions.Repositories;

public interface IMatchRepository
{
    Task SaveAsync(DuelMatch match, CancellationToken cancellationToken);

    Task<DuelMatch?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken);
}
