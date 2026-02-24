# SudokuArena (Skeleton)

## Projects
- `src/SudokuArena.Desktop`: WPF client
- `src/SudokuArena.Server`: REST + SignalR host
- `src/SudokuArena.Sync.Worker`: outbox sync worker
- `src/SudokuArena.Domain`: game rules
- `src/SudokuArena.Application`: use cases
- `src/SudokuArena.Infrastructure`: EF Core + repositories

## Build
```powershell
dotnet restore SudokuArena.slnx --ignore-failed-sources
dotnet build SudokuArena.slnx --no-restore
```

## Run local LAN demo
```powershell
dotnet run --project src/SudokuArena.Server
dotnet run --project src/SudokuArena.Desktop
```

## Docs
- `docs/SudokuArena/Architecture.md`
- `docs/SudokuArena/Deployment.md`
