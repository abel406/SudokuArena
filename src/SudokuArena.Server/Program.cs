using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Contracts;
using SudokuArena.Application.Contracts.Theming;
using SudokuArena.Application.IoC;
using SudokuArena.Domain.Models;
using Microsoft.EntityFrameworkCore;
using SudokuArena.Infrastructure.IoC;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Server.Hubs;
using SudokuArena.Server.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSudokuArenaApplication();
builder.Services.AddSudokuArenaInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SudokuArenaDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS themes (
            Id TEXT NOT NULL CONSTRAINT PK_themes PRIMARY KEY,
            Code TEXT NOT NULL,
            Name TEXT NOT NULL,
            Version INTEGER NOT NULL,
            IsPublished INTEGER NOT NULL,
            IsActive INTEGER NOT NULL,
            Priority INTEGER NOT NULL,
            ValidFromUtc TEXT NULL,
            ValidToUtc TEXT NULL,
            TokensJson TEXT NOT NULL,
            AssetsJson TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE UNIQUE INDEX IF NOT EXISTS IX_themes_Code ON themes (Code);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS media_assets (
            Id TEXT NOT NULL CONSTRAINT PK_media_assets PRIMARY KEY,
            FileName TEXT NOT NULL,
            ContentType TEXT NOT NULL,
            StoragePath TEXT NOT NULL,
            SizeBytes INTEGER NOT NULL,
            CreatedUtc TEXT NOT NULL
        );
        """);
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

app.MapGet("/api/themes/active", async (IThemeService themeService, CancellationToken ct) =>
{
    var active = await themeService.GetActiveAsync(ct);
    return active is null ? Results.NotFound() : Results.Ok(active);
});

app.MapGet("/api/media/{mediaId:guid}", async (Guid mediaId, IMediaAssetService mediaAssetService, CancellationToken ct) =>
{
    var media = await mediaAssetService.OpenReadAsync(mediaId, ct);
    return media is null
        ? Results.NotFound()
        : Results.File(media.Content, media.ContentType, media.FileName);
});

app.MapGet("/api/admin/themes", async (HttpContext httpContext, IThemeService themeService, CancellationToken ct) =>
{
    if (!RoleAuthorization.HasAnyRole(httpContext, "Admin", "Moderator"))
    {
        return Results.Forbid();
    }

    var themes = await themeService.ListAsync(ct);
    return Results.Ok(themes);
});

app.MapPost("/api/admin/themes", async (
    HttpContext httpContext,
    UpsertThemeHttpRequest request,
    IThemeService themeService,
    CancellationToken ct) =>
{
    if (!RoleAuthorization.HasAnyRole(httpContext, "Admin", "Moderator"))
    {
        return Results.Forbid();
    }

    var result = await themeService.UpsertAsync(
        new UpsertThemeRequest(
            request.ThemeId,
            request.Code,
            request.Name,
            request.BaseVersion,
            request.IsPublished,
            request.Priority,
            request.ValidFromUtc,
            request.ValidToUtc,
            request.Tokens,
            request.Assets),
        ct);

    return Results.Ok(result);
});

app.MapPost("/api/admin/themes/{themeId:guid}/activate", async (
    HttpContext httpContext,
    Guid themeId,
    IThemeService themeService,
    CancellationToken ct) =>
{
    if (!RoleAuthorization.HasAnyRole(httpContext, "Admin"))
    {
        return Results.Forbid();
    }

    var activated = await themeService.ActivateAsync(themeId, ct);
    return activated is null ? Results.NotFound() : Results.Ok(activated);
});

app.MapGet("/api/admin/media", async (HttpContext httpContext, IMediaAssetService mediaAssetService, CancellationToken ct) =>
{
    if (!RoleAuthorization.HasAnyRole(httpContext, "Admin", "Moderator"))
    {
        return Results.Forbid();
    }

    var assets = await mediaAssetService.ListAsync(ct);
    return Results.Ok(assets);
});

app.MapPost("/api/admin/media/upload", async (
    HttpContext httpContext,
    IMediaAssetService mediaAssetService,
    CancellationToken ct) =>
{
    if (!RoleAuthorization.HasAnyRole(httpContext, "Admin", "Moderator"))
    {
        return Results.Forbid();
    }

    if (!httpContext.Request.HasFormContentType)
    {
        return Results.BadRequest("Expected multipart/form-data.");
    }

    var form = await httpContext.Request.ReadFormAsync(ct);
    var file = form.Files["file"] ?? form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("Missing file.");
    }

    await using var stream = file.OpenReadStream();
    var uploaded = await mediaAssetService.UploadAsync(
        new UploadMediaRequest(
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            stream,
            "/api/media"),
        ct);

    return Results.Ok(uploaded);
});

app.MapHub<DuelHub>("/hubs/duel");
app.Run();

public sealed record CreateMatchHttpRequest(string HostPlayer, string GuestPlayer, MatchTransport Transport, string? Puzzle);

public sealed record UpsertThemeHttpRequest(
    Guid? ThemeId,
    string Code,
    string Name,
    int? BaseVersion,
    bool IsPublished,
    int Priority,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    Dictionary<string, string>? Tokens,
    Dictionary<string, string>? Assets);
