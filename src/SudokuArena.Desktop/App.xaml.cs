using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SudokuArena.Application.IoC;
using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Theming;
using SudokuArena.Desktop.ViewModels;
using SudokuArena.Infrastructure.IoC;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Puzzles;
using DesktopThemeMode = SudokuArena.Desktop.Theming.ThemeMode;

namespace SudokuArena.Desktop;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSudokuArenaApplication();
                services.AddSudokuArenaInfrastructure(context.Configuration);

                services.AddSingleton<ISystemThemeDetector, WindowsThemeDetector>();
                services.AddSingleton<IThemePreferenceStore, JsonThemePreferenceStore>();
                services.AddSingleton<ThemeManager>();
                services.AddSingleton<IPuzzleProvider>(_ =>
                {
                    var localSeedPath = Path.Combine(AppContext.BaseDirectory, "PuzzleSeed", "puzzles.runtime.v1.json");
                    var serverSeedPath = Path.Combine(AppContext.BaseDirectory, "ServerSeed", "puzzles.runtime.v1.json");

                    var serverSource = new ServerSeedPuzzleProvider(serverSeedPath, required: false);
                    var localSource = new LocalSeedPuzzleProvider(localSeedPath);
                    return new CompositePuzzleProvider(serverSource, localSource);
                });
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host.StartAsync().GetAwaiter().GetResult();

        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SudokuArenaDbContext>();
        dbContext.Database.EnsureCreated();

        var themeManager = _host.Services.GetRequiredService<ThemeManager>();
        var themePreferences = _host.Services.GetRequiredService<IThemePreferenceStore>();
        var requestedThemeMode = themePreferences.LoadThemeMode() ?? DesktopThemeMode.System;
        _ = themeManager.ApplyTheme(requestedThemeMode);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        base.OnExit(e);
    }
}
