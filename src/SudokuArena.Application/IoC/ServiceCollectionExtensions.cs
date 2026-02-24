using Microsoft.Extensions.DependencyInjection;
using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Services;

namespace SudokuArena.Application.IoC;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSudokuArenaApplication(this IServiceCollection services)
    {
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        return services;
    }
}
