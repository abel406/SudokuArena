using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Abstractions.Repositories;

public interface IMediaAssetRepository
{
    Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken);

    Task<MediaAsset?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken cancellationToken);
}
