# Sudoku Arena Architecture (Skeleton v1)

## Stack target
- Runtime: `.NET 10`
- Desktop client: `WPF + MVVM + Generic Host`
- Realtime: `ASP.NET Core SignalR`
- Persistence: `EF Core` with `SQLite` in this skeleton
- Sync: `Outbox` + background worker

## Why this split
- `SudokuArena.Domain`: pure game rules and entities.
- `SudokuArena.Application`: use cases/contracts, no framework coupling.
- `SudokuArena.Infrastructure`: EF Core, repositories, provider switching.
- `SudokuArena.Server`: LAN/cloud realtime host.
- `SudokuArena.Sync.Worker`: local->cloud synchronization.
- `SudokuArena.Desktop`: UX + board interaction.

This structure keeps UI and transport swappable without rewriting game logic.

## Graphics decision for Sudoku board
- Current choice: **native WPF custom drawing** (`FrameworkElement`).
- Reason:
  - No extra rendering dependency.
  - Fast enough for Sudoku (81 cells, low frame complexity).
  - Fine control over hit-testing and keyboard interactions.
- If future requirements include heavy effects/animations, evaluate `SkiaSharp`.

## Data strategy
- Local-first:
  - Desktop/LAN writes to SQLite.
  - Every domain event is added to `outbox_events`.
- Cloud-enabled:
  - Worker pushes pending outbox events to cloud API.
  - Cloud can run PostgreSQL and replay/merge events.
- Benefit:
  - LAN module can run standalone.
  - Cloud sync is optional and late-bound.

## Realtime strategy
- Duel synchronization via SignalR hub (`/hubs/duel`).
- Server is authoritative for move validation.
- Clients render board state sent by server events.

## Theme/media strategy
- Centralized theme management on server with `ThemeManifest`:
  - token dictionary (`color.*`, `font.*`, etc.)
  - asset dictionary (`bg.*`, `icon.*`, etc.)
- Local media file storage (phase 1) exposed via `/api/media/{id}`.
- Admin endpoints allow:
  - create/update themes
  - activate single current theme
  - upload and list media assets
- Role gate (temporary): `X-Role` header (`Admin`, `Moderator`).
- Planned hardening: OAuth/JWT + policy-based authorization.

## Auth strategy (next step)
- LAN-only: local profile without external auth.
- Cloud mode: `Google OAuth` (Gmail) -> backend issues app JWT.
- Keep user identity as `email` plus immutable player id.

## Scalability notes
- Initial model is modular monolith; scale path:
  1. Move matchmaking to dedicated service.
  2. Move ranking/tournaments to separate bounded context.
  3. Add Redis for presence and queueing.

## Immediate next engineering tasks
1. Add migrations and startup seed.
2. Implement tournament aggregates and bracket endpoints.
3. Add reconnect/resync flow in SignalR client.
4. Add Google OAuth in server and desktop login flow.
5. Add PostgreSQL provider package when a stable EF10-compatible version is available.
6. Build installer profiles (LAN-only and Full-cloud).
