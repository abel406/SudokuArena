using Microsoft.EntityFrameworkCore;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Domain.Models;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Repositories;

public sealed class MediaAssetRepository(SudokuArenaDbContext dbContext) : IMediaAssetRepository
{
    public async Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken)
    {
        var entity = await dbContext.MediaAssets.SingleOrDefaultAsync(x => x.Id == asset.Id, cancellationToken);
        if (entity is null)
        {
            entity = new MediaAssetEntity { Id = asset.Id };
            dbContext.MediaAssets.Add(entity);
        }

        entity.FileName = asset.FileName;
        entity.ContentType = asset.ContentType;
        entity.StoragePath = asset.StoragePath;
        entity.SizeBytes = asset.SizeBytes;
        entity.CreatedUtc = asset.CreatedUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.MediaAssets.SingleOrDefaultAsync(x => x.Id == mediaId, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.MediaAssets.ToListAsync(cancellationToken);

        return entities
            .Select(ToModel)
            .OrderByDescending(x => x.CreatedUtc)
            .ToList();
    }

    private static MediaAsset ToModel(MediaAssetEntity entity)
    {
        return new MediaAsset(
            entity.Id,
            entity.FileName,
            entity.ContentType,
            entity.StoragePath,
            entity.SizeBytes,
            entity.CreatedUtc);
    }
}
