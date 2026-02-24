using SudokuArena.Infrastructure.IoC;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Sync.Worker.Configuration;
using SudokuArena.Sync.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSudokuArenaInfrastructure(builder.Configuration);
builder.Services.Configure<CloudSyncOptions>(builder.Configuration.GetSection(CloudSyncOptions.SectionName));
builder.Services.AddHttpClient("cloud-sync");
builder.Services.AddHostedService<OutboxSyncWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SudokuArenaDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
