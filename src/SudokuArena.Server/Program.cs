using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Contracts;
using SudokuArena.Application.IoC;
using SudokuArena.Domain.Models;
using SudokuArena.Infrastructure.IoC;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSudokuArenaApplication();
builder.Services.AddSudokuArenaInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SudokuArenaDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    utcNow = DateTimeOffset.UtcNow
}));

app.MapPost("/api/matches", async (CreateMatchHttpRequest request, IMatchService matchService, CancellationToken ct) =>
{
    var puzzle = string.IsNullOrWhiteSpace(request.Puzzle)
        ? SudokuDefaults.SamplePuzzle
        : request.Puzzle;

    var snapshot = await matchService.CreateAsync(
        new CreateMatchRequest(request.HostPlayer, request.GuestPlayer, request.Transport, puzzle),
        ct);

    return Results.Ok(snapshot);
});

app.MapGet("/api/matches/{matchId:guid}", async (Guid matchId, IMatchService matchService, CancellationToken ct) =>
{
    var snapshot = await matchService.GetAsync(matchId, ct);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapHub<DuelHub>("/hubs/duel");
app.Run();

public sealed record CreateMatchHttpRequest(string HostPlayer, string GuestPlayer, MatchTransport Transport, string? Puzzle);
