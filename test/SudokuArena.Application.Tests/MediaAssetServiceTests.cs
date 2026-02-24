using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Abstractions.Storage;
using SudokuArena.Application.Contracts.Theming;
using SudokuArena.Application.Services;
using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Tests;

public sealed class MediaAssetServiceTests
{
    [Fact]
    public async Task UploadAsync_ShouldStoreMetadataAndReturnPublicUrl()
    {
        var repository = new FakeMediaAssetRepository();
        var storage = new FakeMediaBinaryStorage();
        var service = new MediaAssetService(repository, storage);

        await using var stream = new MemoryStream([1, 2, 3, 4]);
        var result = await service.UploadAsync(
            new UploadMediaRequest("banner.png", "image/png", stream, "/api/media"),
            CancellationToken.None);

        Assert.Equal("banner.png", result.FileName);
        Assert.Equal("image/png", result.ContentType);
        Assert.Contains("/api/media/", result.PublicUrl);
        Assert.True(result.SizeBytes > 0);
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnFileContentWhenMediaExists()
    {
        var repository = new FakeMediaAssetRepository();
        var storage = new FakeMediaBinaryStorage();
        var service = new MediaAssetService(repository, storage);

        await using (var uploadStream = new MemoryStream([9, 8, 7]))
        {
            await service.UploadAsync(
                new UploadMediaRequest("logo.svg", "image/svg+xml", uploadStream, "/api/media"),
                CancellationToken.None);
        }

        var media = (await service.ListAsync(CancellationToken.None)).Single();
        var content = await service.OpenReadAsync(media.MediaId, CancellationToken.None);

        Assert.NotNull(content);
        Assert.Equal("image/svg+xml", content!.ContentType);
        Assert.Equal("logo.svg", content.FileName);
    }

    private sealed class FakeMediaAssetRepository : IMediaAssetRepository
    {
        private readonly Dictionary<Guid, MediaAsset> _assets = new();

        public Task SaveAsync(MediaAsset asset, CancellationToken cancellationToken)
        {
            _assets[asset.Id] = asset;
            return Task.CompletedTask;
        }

        public Task<MediaAsset?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken)
        {
            _assets.TryGetValue(mediaId, out var asset);
            return Task.FromResult(asset);
        }

        public Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<MediaAsset>>(_assets.Values.ToList());
        }
    }

    private sealed class FakeMediaBinaryStorage : IMediaBinaryStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();

        public async Task<StoredMediaFile> SaveAsync(
            Guid mediaId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            await using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();
            var path = $"{mediaId:D}{Path.GetExtension(fileName)}";
            _files[path] = bytes;
            return new StoredMediaFile(path, bytes.Length);
        }

        public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_files.TryGetValue(storagePath, out var bytes))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        }
    }
}
