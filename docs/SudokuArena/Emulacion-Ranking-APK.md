# Emulacion de Ranking APK (sin root)

Fecha: 2026-02-26

## Objetivo

Poder estimar ranking y score agregado con las reglas ya confirmadas del APK, sin necesidad de extraer `Sudoku.db` del dispositivo.

Implementacion:
- `src/SudokuArena.Application/Scoring/ApkScoreRankingEmulator.cs`
- tests: `test/SudokuArena.Application.Tests/ApkScoreRankingEmulatorTests.cs`

## Que replica

- Ventana tipo `v1` (`rf/v0.v1`):
  - estado completado (`state = 15`)
  - rango temporal exclusivo (`start < lastOperationTime < end`)
  - gameType candidatos (`NORMAL/DC/ACTIVE/BATTLE`)
  - filtro final: solo `NORMAL/DC/ACTIVE`
  - score valido: `oldScore >= 0`

- Ventana tipo `w1` (`rf/v0.w1`):
  - estado completado (`state = 15`)
  - rango temporal exclusivo
  - candidatos `NORMAL/DC/ACTIVE/BATTLE` + `DAILY` con `sudokuType in (1,2)`
  - score valido: `oldScore > 0`
  - soporte para reglas AB via callbacks:
    - `IsBattleScoreTypeMode(int gameType)`
    - `IsExploreScoreTypeMode(int gameType, int sudokuType)`

- Regla old scoring:
  - `scoreVersion == null || scoreVersion == 0`

## API util

- `ComputeV1WindowSummary(...)`
- `ComputeW1WindowSummary(...)`
- `BuildW1Leaderboard(...)`
- `IsOldScoring(int? scoreVersion)`

## Ejemplo rapido

```csharp
var emulator = new ApkScoreRankingEmulator();
var start = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
var end = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

var leaderboard = emulator.BuildW1Leaderboard(games, start, end);
```

## Limite conocido

Sin BD real del APK no se puede validar distribucion real de `scoreVersion=1`; esta emulacion queda como baseline operativo hasta cerrar esa evidencia.
