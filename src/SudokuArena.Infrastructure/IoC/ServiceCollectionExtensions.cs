using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Abstractions.Storage;
using SudokuArena.Infrastructure.Configuration;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Repositories;
using SudokuArena.Infrastructure.Storage;

namespace SudokuArena.Infrastructure.IoC;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSudokuArenaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new DataStoreOptions();
        configuration.GetSection(DataStoreOptions.SectionName).Bind(options);
        var mediaOptions = new MediaStorageOptions();
        configuration.GetSection(MediaStorageOptions.SectionName).Bind(mediaOptions);

        services.AddSingleton(options);
        services.AddSingleton(mediaOptions);
        services.AddDbContext<SudokuArenaDbContext>(builder =>
        {
            // For this skeleton we keep SQLite as default provider in every profile.
            // Cloud sync is handled through the outbox worker and cloud API.
            EnsureSqliteDirectoryExists(options.LocalSqliteConnectionString);
            builder.UseSqlite(options.LocalSqliteConnectionString);
        });

        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IThemeRepository, ThemeRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IMediaBinaryStorage, FileSystemMediaBinaryStorage>();

        return services;
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource) || sqliteBuilder.DataSource == ":memory:")
        {
            return;
        }

        var fullPath = Path.GetFullPath(sqliteBuilder.DataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
