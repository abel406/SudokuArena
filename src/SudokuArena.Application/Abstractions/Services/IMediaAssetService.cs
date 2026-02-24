using SudokuArena.Application.Contracts.Theming;

namespace SudokuArena.Application.Abstractions.Services;

public interface IMediaAssetService
{
    Task<MediaAssetSnapshot> UploadAsync(UploadMediaRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaAssetSnapshot>> ListAsync(CancellationToken cancellationToken);

    Task<MediaAssetSnapshot?> GetAsync(Guid mediaId, CancellationToken cancellationToken);

    Task<MediaAssetContent?> OpenReadAsync(Guid mediaId, CancellationToken cancellationToken);
}
