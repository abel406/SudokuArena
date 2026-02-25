# Puzzle Dataset Schema v1

ID: `sudokuarena.puzzle_dataset.v1`

## Objetivo
Contrato unico de dataset para consumo en Desktop/Server (runtime) y salida de `tools/puzzle_dataset`.

## Estructura
- `schema_version` (string): version del contrato.
- `generated_at_utc` (ISO-8601 UTC): fecha de generacion del lote.
- `question_bank` (array): puzzles base.
- `solver_details` (object): `puzzle_id -> metricas de dificultad`.
- `time_map` (object): `puzzle_id -> [t1,t2,t3,t4]` en segundos.

## question_bank[]
- `puzzle_id` (string)
- `board_kind` (enum string, opcional, default `Classic9x9`): `Classic9x9|SixBySix|SixteenBySixteen`
- `mode` (enum string, opcional): `Beginner|Easy|Medium|Hard|Expert|Extreme|Six|Sixteen|Unknown`
- `puzzle` (string):
  - `Classic9x9`: 81 chars (`1-9` y `0`/`.` para vacios)
  - `SixBySix`: 36 chars (`1-6` y `0`/`.`)
  - `SixteenBySixteen`: 256 chars (`1-9A-G` y `0`/`.`)
- `solution` (string):
  - `Classic9x9`: 81 chars (`1-9`)
  - `SixBySix`: 36 chars (`1-6`)
  - `SixteenBySixteen`: 256 chars (`1-9A-G`)
- `difficulty_tier` (enum string): `Beginner|Easy|Medium|Hard|Expert`
- `given_count` (int)

## solver_details[puzzle_id]
- `weighted_se` (number)
- `max_rate` (int)
- `advanced_hits` (int)
- `technique_counts` (object string->int)

## time_map[puzzle_id]
- array de 4 enteros: `[t1,t2,t3,t4]`

## Ejemplo minimo
```json
{
  "schema_version": "sudokuarena.puzzle_dataset.v1",
  "generated_at_utc": "2026-02-25T00:00:00Z",
  "question_bank": [
    {
      "puzzle_id": "puz-0001",
      "board_kind": "Classic9x9",
      "mode": "Medium",
      "puzzle": "53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79",
      "solution": "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
      "difficulty_tier": "Medium",
      "given_count": 30
    }
  ],
  "solver_details": {
    "puz-0001": {
      "weighted_se": 2.7,
      "max_rate": 4,
      "advanced_hits": 1,
      "technique_counts": {
        "single": 38,
        "hidden_single": 11
      }
    }
  },
  "time_map": {
    "puz-0001": [120, 180, 260, 360]
  }
}
```
