# SudokuArena (Skeleton)

## Projects
- `src/SudokuArena.Desktop`: WPF client
- `src/SudokuArena.Server`: REST + SignalR host
- `src/SudokuArena.Sync.Worker`: outbox sync worker
- `src/SudokuArena.Domain`: game rules
- `src/SudokuArena.Application`: use cases
- `src/SudokuArena.Infrastructure`: EF Core + repositories

## Quick Start (Recommended)
Run everything with one script:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\run-local.ps1
```

What this does:
- Restore packages
- Build solution
- Run tests
- Start server (`http://localhost:5055`)
- Start WPF desktop app

When you close the desktop app, the server process is stopped automatically.

### Script options
```powershell
# Start only server
pwsh -ExecutionPolicy Bypass -File .\scripts\run-local.ps1 -ServerOnly

# Skip restore/build/tests if already done
pwsh -ExecutionPolicy Bypass -File .\scripts\run-local.ps1 -SkipRestore -SkipBuild -SkipTests

# Custom server URL
pwsh -ExecutionPolicy Bypass -File .\scripts\run-local.ps1 -ServerUrl http://localhost:6060
```

## Manual Steps
```powershell
dotnet restore SudokuArena.slnx
dotnet build SudokuArena.slnx --no-restore
dotnet test SudokuArena.slnx --no-build
```

Start server:
```powershell
dotnet run --project src/SudokuArena.Server --urls http://localhost:5055
```

Basic API smoke test:
```powershell
curl.exe -s http://localhost:5055/api/health
curl.exe -s -X POST http://localhost:5055/api/matches -H "Content-Type: application/json" -d "{\"hostPlayer\":\"p1@gmail.com\",\"guestPlayer\":\"p2@gmail.com\",\"transport\":0}"
```

Start desktop:
```powershell
dotnet run --project src/SudokuArena.Desktop
```

## Docs
- `docs/SudokuArena/Architecture.md`
- `docs/SudokuArena/Deployment.md`
- `docs/SudokuArena/Registro-Avance-Ideas.md`

## Theme and Media Management API (Phase 1)
Role header (temporary auth placeholder):
- `X-Role: Admin` or `X-Role: Moderator`

Public endpoints:
- `GET /api/themes/active`
- `GET /api/media/{mediaId}`

Admin endpoints:
- `GET /api/admin/themes`
- `POST /api/admin/themes`
- `POST /api/admin/themes/{themeId}/activate` (Admin only)
- `GET /api/admin/media`
- `POST /api/admin/media/upload` (`multipart/form-data`, file field: `file`)

Example create theme:
```powershell
curl.exe -s -X POST http://localhost:5055/api/admin/themes `
  -H "Content-Type: application/json" `
  -H "X-Role: Admin" `
  -d "{\"themeId\":null,\"code\":\"halloween-2026\",\"name\":\"Halloween 2026\",\"baseVersion\":null,\"isPublished\":true,\"priority\":50,\"validFromUtc\":null,\"validToUtc\":null,\"tokens\":{\"color.primary\":\"#ff6a00\"},\"assets\":{\"bg.main\":\"/api/media/placeholder\"}}"
```
