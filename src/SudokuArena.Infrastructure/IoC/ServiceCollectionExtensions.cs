using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Infrastructure.Configuration;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Repositories;

namespace SudokuArena.Infrastructure.IoC;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSudokuArenaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new DataStoreOptions();
        configuration.GetSection(DataStoreOptions.SectionName).Bind(options);

        services.AddSingleton(options);
        services.AddDbContext<SudokuArenaDbContext>(builder =>
        {
            // For this skeleton we keep SQLite as default provider in every profile.
            // Cloud sync is handled through the outbox worker and cloud API.
            builder.UseSqlite(options.LocalSqliteConnectionString);
        });

        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }
}
