# Matriz tecnica: referencia -> implementacion SudokuArena

Fecha: 2026-02-25  
Base: hallazgos en `docs/SudokuArena/Analisis-Referencia-EasySudoku.md` (secciones 15, 16 y 17)
Leyenda de siglas compartida: `docs/SudokuArena/Leyenda-Siglas.md`

Objetivo:
- convertir hallazgos del decompilado en tareas de implementacion concretas para `Domain`, `Application`, `Infrastructure`, `Server` y `Desktop`.

Convencion:
- `Feature`: capacidad funcional.
- `Dato`: que comportamiento/modelo se debe preservar (conceptualmente).
- `Fuente`: evidencia tecnica en app referencia.
- `Como implementarlo en SudokuArena`: punto de entrada recomendado en este repo.

| Feature | Dato (referencia) | Fuente (Easy Sudoku) | Como implementarlo en SudokuArena |
|---|---|---|---|
| Reloj de partida | Tick cada 1s que incrementa tiempo de juego. | `sources/com/meevii/sudoku/plugin/SudokuTime.java:63`, `:76` | Crear `GameClock` en `src/SudokuArena.Domain/Models` o `src/SudokuArena.Application` y mover logica de `TickClock()` de [MainViewModel.cs](../../src/SudokuArena.Desktop/ViewModels/MainViewModel.cs). Persistir en snapshot de match. |
| Tiempo entre jugadas | Metricas separadas: tiempo total y tiempo sin fill (`timeNoFill`, `timeNoFillRight`). | `sources/com/meevii/sudoku/plugin/SudokuTime.java:78-79` | Extender `MatchSnapshot` con `ElapsedSeconds`, `IdleSeconds`, `IdleSinceLastCorrectSeconds` en `src/SudokuArena.Application/Contracts/MatchSnapshot.cs`; actualizar `MatchService` y `MatchRepository`. |
| Pausa/reanudar | Estados timer `BEGIN/CONTINUE/PAUSE/END` y pausa desde control. | `sources/com/meevii/sudoku/plugin/SudokuTime.java:38`, `sources/com/meevii/sudoku/SudokuControl.java:630` | Agregar estado de reloj en dominio (`Running/Paused/Finished`) y comandos de pausa en API/hub (`Program.cs`, `DuelHub.cs`) para LAN/cloud. |
| Limite de errores | `mistake`, `totalMistake`, `limitMistake` separados. | `sources/com/meevii/data/bean/GameData.java:200`, `:753`, `:925` | Crear `MatchProgress` (nuevo modelo) con `ErrorCount`, `TotalErrorCount`, `ErrorLimit`; integrar con validacion de jugadas en `DuelMatch.RegisterMove()`. |
| Estado visual de celdas con error | Mapa serializable por celda (`mistakeCellState`). | `sources/com/meevii/data/bean/GameData.java:209`, `:1532-1533` | Guardar `InvalidCells`/`ErrorMask` en `MatchEntity` (nuevo campo JSON) y reflejar en [SudokuBoardControl.cs](../../src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs). |
| Score por tiempo y errores | Score usa coeficientes por dificultad + bonus por errores remanentes y tiempo. | `sources/com/meevii/sudoku/plugin/SudokuScore.java:35-42`, `:147-182` | Introducir `IScoringPolicy` en `Application`; implementar `ScoringPolicyV1` configurable (json) y aplicarla en `MatchService.SubmitMoveAsync()`. |
| Umbrales de tiempo por puzzle | `scoreTimeMap` define buckets de score (10/8/6/4/2). | `sources/com/meevii/sudoku/plugin/SudokuScore.java:236-247`, `resources/assets/config/question_time_map.json` | En dataset propio (`tools/puzzle_dataset`), exportar `time_map` por puzzle y cargarlo en servicio de score del servidor. |
| Celdas given/editable | Regla fuerte: celda given no se edita; borrar solo editable. | `sources/com/meevii/sudoku/SudokuControl.java:688`, `sources/com/meevii/data/bean/GameData.java:349` | Mantener invariantes de `SudokuBoard.TrySetCell()` ([SudokuBoard.cs](../../src/SudokuArena.Domain/Models/SudokuBoard.cs)) y exponer `EditableCells` tambien en snapshot API para clientes remotos. |
| Historial de jugadas (undo) | Cada jugada guarda record para deshacer/analitica. | `sources/com/meevii/data/bean/GameData.java:476`, `sources/com/meevii/sudoku/SudokuControl.java:750` | Persistir movimientos en tabla nueva `match_moves` (orden, jugador, celda, valor previo/nuevo, timestamp). Usar para undo server-authoritative y replay. |
| Config de gameplay | Toggles: mistakes limit, number-first, light mode, highlights, smart hint, sound, vibration. | `sources/com/meevii/ui/activity/SettingActivity.java:332`, `:712`, `:772`, `:778`, `:784`, `:796`, `:826`, `:832` | Crear `PlayerSettings` en dominio + persistencia (`player_settings` en EF). Consumir desde `MainViewModel` y exponer endpoint `/api/settings`. |
| Tokens de color de tablero | Paleta por tokens (`chessboardBgSelectColor`, etc.) en vez de hardcode. | `resources/res/values/attrs.xml:814-836`, `resources/res/values/styles.xml:4447-4458` | Reemplazar constantes en [SudokuBoardControl.cs](../../src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs) por tokens de `ThemeManifest` (`src/SudokuArena.Domain/Models/ThemeManifest.cs`). |
| Perfil de jugador | Flags de perfil + nombre competitivo (battle username). | `sources/ic/t0.java:1158-1159`, `sources/ic/y0.java:40`, `:54`, `:62` | Persistir `PlayerProfile` ([PlayerProfile.cs](../../src/SudokuArena.Domain/Models/PlayerProfile.cs)) en DB con `DisplayName`, `CountryCode`, `Avatar`, `CanEditProfile`; publicar endpoints profile. |
| Preferencias locales | SharedPreferences central para estado de usuario. | `sources/com/meevii/common/utils/k1.java:311` | Mantener equivalente local en Desktop (`appsettings` + store local sqlite/key-value). Sincronizar subset al servidor cuando haya sesion cloud. |
| Persistencia de partidas | DB principal guarda estado de juego y progreso de temporada. | `sources/xf/j.java:525`, `:157`, `:185`, `:389` | Extender `MatchEntity` ([MatchEntity.cs](../../src/SudokuArena.Infrastructure/Persistence/Entities/MatchEntity.cs)) con campos de progreso y crear entidades de season/rank cuando entre feature torneos. |
| Cache JSON efimero | Archivos locales para caches de actividades/temporadas. | `sources/ic/i.java:239`, `:502`, `:951`, `sources/ic/t0.java:133`, `:276`, `:592` | Definir capa `LocalCache` en `Infrastructure` para datos efimeros no criticos (json en disco con TTL), separada de datos transaccionales. |
| Sync de gameData | Endpoint dedicado GET/POST para sincronizar progreso de usuario. | `sources/qf/a.java:22`, `:28` | Agregar `/api/sync/game-data` en `Server` y contrato `GameDataSyncDto`; usar `OutboxSyncWorker` para reconciliacion cloud. |
| Snapshot por zip | Flujo de descarga/subida de snapshot comprimido (`syncRemoteData.zip`, `syncData`). | `sources/rf/y0.java:69`, `:89`, `:142-143`, `:151` | Implementar export/import de snapshot de cuenta (json + zip) para modo offline/LAN->cloud. Integrar con `SudokuArena.Sync.Worker`. |
| Mensajes por jugada al servidor | Cada movimiento se envia y se propaga por tiempo real. | `sources/qf/a.java:28`, `sources/com/meevii/sudoku/SudokuControl.java` (flujo de fill/erase) | Ya existe base en [DuelHub.cs](../../src/SudokuArena.Server/Hubs/DuelHub.cs); siguiente paso: incluir metadata (`playedUtc`, `clientSeq`, `isDelete`, `errorDelta`, `elapsed`) en `MoveSubmission`. |
| Dataset de dificultad | Banco + solver details + time map para clasificar y puntuar puzzles. | `resources/assets/config/defaultQb*.json`, `defaultQbSolverDetails*.json`, `question_time_map.json` | Implementar `tools/puzzle_dataset` propio y versionado (`schema_version`, `weights_version`, `thresholds_version`) y cargar dataset desde servidor. |

## Priorizacion sugerida (MVP online LAN + sync)

1. Reloj y progreso de errores server-authoritative (filas 1-5).  
2. Movimiento enriquecido + historial persistido (filas 9 y 18).  
3. Settings y tema por tokens (filas 10-11).  
4. Sync de gameData + snapshot export/import (filas 16-17).  
5. Dataset propio y score calibrado (filas 6-8 y 19).
