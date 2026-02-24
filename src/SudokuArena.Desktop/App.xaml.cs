using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SudokuArena.Application.IoC;
using SudokuArena.Desktop.ViewModels;
using SudokuArena.Infrastructure.IoC;
using SudokuArena.Infrastructure.Persistence;

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
