using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Abstractions.Storage;
using SudokuArena.Application.Contracts.Theming;
using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Services;

public sealed class MediaAssetService(
    IMediaAssetRepository mediaAssetRepository,
    IMediaBinaryStorage mediaBinaryStorage) : IMediaAssetService
{
    public async Task<MediaAssetSnapshot> UploadAsync(UploadMediaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("FileName is required.", nameof(request));
        }

        if (request.Content is null || !request.Content.CanRead)
        {
            throw new ArgumentException("Content stream is required.", nameof(request));
        }

        var mediaId = Guid.NewGuid();
        var stored = await mediaBinaryStorage.SaveAsync(mediaId, request.FileName, request.Content, cancellationToken);
        var createdUtc = DateTimeOffset.UtcNow;

        var asset = new MediaAsset(
            mediaId,
            request.FileName,
            request.ContentType,
            stored.StoragePath,
            stored.SizeBytes,
            createdUtc);

        await mediaAssetRepository.SaveAsync(asset, cancellationToken);
        return ToSnapshot(asset, request.PublicUrlBase);
    }

    public async Task<IReadOnlyList<MediaAssetSnapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var assets = await mediaAssetRepository.ListAsync(cancellationToken);
        return assets
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => ToSnapshot(x, "/api/media"))
            .ToList();
    }

    public async Task<MediaAssetSnapshot?> GetAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await mediaAssetRepository.GetByIdAsync(mediaId, cancellationToken);
        return media is null ? null : ToSnapshot(media, "/api/media");
    }

    public async Task<MediaAssetContent?> OpenReadAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var media = await mediaAssetRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media is null)
        {
            return null;
        }

        var content = await mediaBinaryStorage.OpenReadAsync(media.StoragePath, cancellationToken);
        if (content is null)
        {
            return null;
        }

        return new MediaAssetContent(content, media.ContentType, media.FileName);
    }

    private static MediaAssetSnapshot ToSnapshot(MediaAsset asset, string publicUrlBase)
    {
        return new MediaAssetSnapshot(
            asset.Id,
            asset.FileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.CreatedUtc,
            $"{publicUrlBase.TrimEnd('/')}/{asset.Id:D}");
    }
}
