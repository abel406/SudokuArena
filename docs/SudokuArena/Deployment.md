# Sudoku Arena Deployment Profiles

## Profile A: LAN-only (fully open source)
Installables:
- `SudokuArena.Desktop`
- `SudokuArena.Server`
- `SudokuArena.Sync.Worker` (optional, can remain disabled)

Infra:
- SQLite file only (`./data/*.db`)
- No cloud dependency
- SignalR over local network (host machine IP)

Typical command run:
```powershell
dotnet run --project src/SudokuArena.Server
dotnet run --project src/SudokuArena.Desktop
```

## Profile B: Hybrid LAN + Cloud Sync
Same as Profile A, plus:
- Configure `CloudSync.BaseUrl` and token in worker.
- Run worker as background service.

Flow:
1. Local matches are stored in SQLite.
2. Outbox events are sent to cloud endpoint.
3. Cloud persists in PostgreSQL and updates global ranking/tournaments.

## Profile C: Cloud-first
Server config:
- `DataStore.Mode = CloudPostgres`
- PostgreSQL connection string enabled (pending provider integration)

Use this when you need multi-region users and centralized tournaments.

## Installer strategy
- Use `dotnet publish` single-file self-contained per module.
- Build two installers:
  - `SudokuArena-LAN`: Desktop + Server
  - `SudokuArena-Full`: Desktop + Server + Worker + cloud setup wizard

## Configuration keys
- `DataStore.Mode`: `LocalSqlite` | `CloudPostgres`
- `DataStore.LocalSqliteConnectionString`
- `DataStore.CloudPostgresConnectionString` (placeholder in skeleton)
- `CloudSync.BaseUrl`
- `CloudSync.ApiToken`
- `Realtime.HubUrl` (desktop)
