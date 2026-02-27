# Analisis BD + Scoring (APK Easy Sudoku)

Fecha: 2026-02-26

## 1) Resumen ejecutivo

- El calculo de score (`old/new/final`) es runtime en `SudokuScore`, no una comparacion contra historico en BD.
- La BD `SudokuGame` persiste `score` (old), `newScore` y `scoreVersion`, y luego se usa para historico, estadisticas y agregados (por ejemplo torneo).
- Existe detector explicito de "old scoring" por historico (`scoreVersion == null || 0`) en repositorio/UI de resultados.
- Se recupero la version `show-bad-code` del metodo `oldScore` y se guardo en decomp para trazabilidad.

## 2) Artefactos guardados

- Fuente recuperada de `SudokuScore` con `--show-bad-code`:
  - `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.show-bad-code.java`
- Metodo clave:
  - `F(GameData, int extra, boolean isRight)` en `.../SudokuScore.show-bad-code.java:517`

## 3) Modelo de datos de score en BD

- Tabla principal:
  - `SudokuGame` (Room)
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/db/SudokuGameDataBase_Impl.java:210`
- Columnas de score:
  - `score` (old), `newScore`, `scoreVersion`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pf/f.java:121`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pf/f.java:125`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pf/f.java:129`

## 4) Mapeo GameData <-> SudokuGame

- `fromEntity` restaura `old/new/scoreVersion` desde BD:
  - `gameData.setOldScore(fVar.q())`
  - `gameData.setNewScore(fVar.n())`
  - `gameData.setScoreVersion(fVar.r())`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:407`
- `toEntity` persiste `old/new/scoreVersion` hacia BD:
  - `fVar.V(getOldScore())`
  - `fVar.R(getNewScore())`
  - `fVar.W(getScoreVersion())`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:456`
- Selector final:
  - `finalScore = (scoreVersion == 1 ? newScore : oldScore)`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:650`

## 5) Calculo de score (runtime)

- Flujo de jugada:
  - `a(...)` actualiza score por fill y emite `ScoreChangeBean`.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.java:480`
- Cierre de partida:
  - `k()` aplica extras finales.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.java:517`
- `oldScore` (recuperado en show-bad-code):
  - acierto con `t=min(time,120)`:
    - `SIX: 180 - t`
    - `BEGINNER/EASY: 270 - t`
    - `MEDIUM: 420 - 2t`
    - `HARD: 760 - 3t`
    - `EXPERT: 1120 - 4t`
    - `EXTREME/SIXTEEN: 1320 - 4t`
  - error:
    - `SIX=-60`, `BEGINNER/EASY=-150`, `MEDIUM=-180`, `HARD=-400`, `EXPERT/SIXTEEN=-640`, `EXTREME=-840`
  - update:
    - `oldScore = oldScore + delta + extra`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.show-bad-code.java:517`

## 6) Persistencia (DAO / Room) y lo que SI se usa para historico

- `INSERT/UPDATE` de `SudokuGame` incluyen `score/newScore/scoreVersion`:
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/of/n.java:186`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/of/n.java:343`
- `SudokuGameDao` (`of.m`) consulta principalmente por:
  - ventanas de tiempo, estado, modo, tipo, counts de perfeccion y tiempos.
  - no se observa recalculo de score desde historico para la partida actual.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/of/m.java:71`
- Agregado de torneo (score historico):
  - el repositorio suma `fVar.q()` (columna `score`) sobre partidas completadas en rango.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1085`

## 7) "Comparacion" detectada y por que no cambia el algoritmo de score

- Se detecto comparacion de `oldScore` en control de estado/undo:
  - `if (currentOldScore < snapshotOldScore) snapshotOldScore = currentOldScore`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/SudokuControl.java:1069`
- Interpretacion:
  - es una normalizacion de snapshot (control local de estado), no una formula de score contra historico de BD.

## 8) Hallazgos de tablas cercanas

- `tournament_season` guarda `userScore`/`userRank` por temporada (no sustituye el score por jugada de `SudokuGame`).
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pf/g.java:31`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/of/o.java:30`

## 9) Pendiente abierto

- Falta cerrar evidencia de activacion runtime de `scoreVersion=1`:
  - en el bloque inspeccionado no aparece invocacion externa a `setScoreVersion(...)` fuera de deserializacion.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:414`

## 10) Paso siguiente cerrado (runtime de `scoreVersion`)

- Escritura local directa de `scoreVersion`:
  - `GameData` inicia en `scoreVersion = 0`.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:340`
  - No se observo setter externo a `GameData.fromEntity(...)` en el codigo Java decomp inspeccionado.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:414`

- Creacion de partida nueva:
  - Repositorio crea `GameData` nuevo (`y1`) y no marca `scoreVersion`.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1837`
  - Inicializacion de score se hace con `setAllScore(...)` (escribe `old/new` en paralelo, no version).
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:1271`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/vj/g1.java:1462`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/SudokuControl.java:1186`

- Runtime de scoring:
  - `SudokuScore` actualiza `oldScore/newScore`, pero no se encontro set de `scoreVersion`.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.java:129`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/sudoku/plugin/SudokuScore.java:135`
  - Selector final sigue en `GameData.getFinalScore()`.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/com/meevii/data/bean/GameData.java:650`

- Persistencia local JSON (cache de partida):
  - Guardado/lectura por Gson con `@Expose`; `scoreVersion` puede viajar por cache aunque no se setee en runtime local.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/lf/e0.java:139`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/lf/e0.java:315`

- Detector de "old scoring" sobre historico:
  - `rf/v0.q0()` filtra partidas con `scoreVersion == null || 0` (excluye `cg.h.p()`).
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1708`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1717`
  - `GameResultViewModel` expone `G()` usando ese filtro.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/vj/u.java:486`

- Observacion adicional:
  - Agregados de torneo en repositorio (`u1 -> w1/v1`) siguen sumando `fVar.q()` (`score` old) por rango.
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1061`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1084`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1783`

## 11) Limite tecnico actual para trazado final

- Intento de trazado smali bloqueado por tooling local:
  - `apktool-cli-2.10.0.jar` existente en `artifacts/tools` esta corrupto/no ejecutable.
  - intento alternativo de instalacion (`choco install apktool`) no completo en el timeout de esta sesion.
- Siguiente paso recomendado cuando el tooling quede operativo:
  - buscar en smali cualquier `iput`/`invoke-virtual` que escriba `GameData.scoreVersion` o llame `setScoreVersion`.

## 12) Verificacion smali global (apktool 2.12.1) - cerrada

- Se decompilo el `base.apk` completo con `apktool 2.12.1`:
  - salida: `artifacts/decomp/easy.sudoku.puzzle.solver.free_apktool_2.12.1_20260226`
- Evidencia smali sobre `scoreVersion`:
  - `scoreVersion` field en `GameData`: `.../smali_classes9/com/meevii/data/bean/GameData.smali:482`
  - default `scoreVersion = 0` (constructor): `.../GameData.smali:654`
  - `getFinalScore()` lee `scoreVersion` para selector: `.../GameData.smali:3411`
  - setter `setScoreVersion(I)V`: `.../GameData.smali:7321`
  - llamada a `setScoreVersion(I)V` localizada en `fromEntity` (lee `pf.f.r()`): `.../GameData.smali:1486`
- Busqueda global en todas las carpetas `smali*`:
  - `setScoreVersion(I)V` solo aparece en `GameData.smali` (definicion + llamada interna en `fromEntity`).
  - referencia directa a field `GameData;->scoreVersion:I` solo aparece en `GameData.smali` (2 lecturas + 2 escrituras internas).

Conclusion tecnica actual:
- no hay evidencia de escritura externa (en smali global del APK) que fuerce `scoreVersion=1` en runtime local;
- el valor sigue entrando por persistencia/entidad (`fromEntity`) o por serializacion previa del propio objeto.

## 13) Flujo de sync remoto (game_data.json) y efecto sobre `scoreVersion`

Hallazgo central:
- el pipeline de sync remoto usa `pj.b` (`RemoteJsonGameData`) y no incluye `scoreVersion` en su contrato JSON.

Evidencias:
- `pj.b` exporta/importa `oldScore/newScore`:
  - write JSON: `put("newScore", ...)` y `put("oldScore", ...)`
  - read JSON: `optInt("newScore", -1)` y `optInt("oldScore", -1)`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pj/b.java:253`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pj/b.java:269`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pj/b.java:382`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pj/b.java:390`
  - En smali de `pj/b` solo aparecen literales `newScore` y `oldScore` (sin literal `scoreVersion`):
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_apktool_2.12.1_20260226/smali_classes9/pj/b.smali:1876`
- `pj.b.c(...)` -> `pf.f` (entidad DB) asigna `score/newScore` pero no llama `W(...)` (`scoreVersion`):
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/pj/b.java:98`
- el manager de sync (`qj.n`) serializa/deserializa `game_data.json` con `pj.b`:
  - carga remoto/local: `new pj.b(new JSONObject(...))`
  - guarda merged: `z(this.f106770b)` con `entry.getValue().l()` (JSONObject de `pj.b`)
  - inserta en DB: `rf.v0.J1(arrayList)` -> `pj.b.d(list)` -> `List<pf.f>` -> DAO
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/qj/n.java:998`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/qj/n.java:806`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/qj/n.java:900`
  - Evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/rf/v0.java:1245`

Consecuencia tecnica:
- por sync remoto, `scoreVersion` no viaja (ni upload ni download) en este build;
- al rehidratar `GameData` desde entidad, si `fVar.r()` es `null`/`0`, `GameData` queda con default `0`.

## 14) Estado final del objetivo `scoreVersion=1` (flujo completo actual)

Con la evidencia Java + smali + sync:
- no se encontro writer runtime local ni en smali global que establezca `scoreVersion=1`;
- el canal de sync remoto actual tampoco transporta `scoreVersion`;
- por lo tanto, en este APK build, `scoreVersion=1` solo puede aparecer si ya venia persistido desde una fuente externa previa (p. ej. datos legado/externos), no por generacion normal del runtime inspeccionado.

## 15) Siguiente paso recomendado (evidencia en dispositivo real)

Objetivo:
- medir si existen filas reales con `scoreVersion=1` y desde cuando aparecen.

Dato cerrado de configuracion Room:
- nombre de BD: `Sudoku.db`
- package: `easy.sudoku.puzzle.solver.free`
- evidencia: `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916/sources/xf/j.java:525`

Script preparado:
- `scripts/pull-easy-sudoku-db.ps1`
- extrae por `adb exec-out run-as`:
  - `Sudoku.db`
  - `Sudoku.db-wal` (si existe)
  - `Sudoku.db-shm` (si existe)
- destino:
  - `artifacts/device-db/easy.sudoku.puzzle.solver.free_yyyyMMdd_HHmmss`

Uso:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pull-easy-sudoku-db.ps1`

Queries para auditoria inmediata:
- distribucion por version:
  - `select coalesce(scoreVersion,-1) as scoreVersion, count(*) as cnt from SudokuGame group by coalesce(scoreVersion,-1) order by scoreVersion;`
- muestra de partidas con version 1:
  - `select id, time, mode, type, score, newScore, scoreVersion from SudokuGame where scoreVersion = 1 order by time desc limit 50;`
- contraste old/new en version 1:
  - `select count(*) as total, sum(case when score = newScore then 1 else 0 end) as same_score, sum(case when score <> newScore then 1 else 0 end) as diff_score from SudokuGame where scoreVersion = 1;`

Resultado empirico actual (2026-02-26, dispositivo `SM-A556E`):
- `adb devices` detecta equipo, pero `run-as` falla en app release:
  - `run-as: package not debuggable: easy.sudoku.puzzle.solver.free`
- intento alternativo con `adb backup` disponible, pero la app excluye la BD:
  - `res/xml/backup_content.xml` excluye `domain="database" path="Sudoku.db"`
  - `res/xml/data_extraction_rules.xml` excluye `domain="database" path="sudoku.db"` en `cloud-backup` y `device-transfer`
- conclusion operativa:
  - sin root o sin build debuggable/repackageado, no se puede extraer `Sudoku.db` de este dispositivo para cerrar distribucion real de `scoreVersion`.

## 16) Intento alternativo en Google Play Games (Windows)

Contexto:
- la app instalada en Google Play Games usa VM `crosvm` con disco local:
  - `%LOCALAPPDATA%\\Google\\Play Games\\userdata_05oohsf2.yfz\\avd\\userdata.img`

Hallazgos:
- en runtime de esta sesion, logs de arranque muestran:
  - `androidboot.kiwi.adbproxy.enabled=0`
  - `androidboot.kiwi.adbproxy.port=38068`
- `adb connect 127.0.0.1:38068` falla (conexion denegada).
- con Play Games cerrado, el archivo `userdata.img` es legible, pero:
  - no contiene firmas reconocibles (`ext4`, `qcow2`, `Android sparse`) ni `SQLite format 3`
  - `file` en WSL lo clasifica como `data`
  - busqueda binaria no encontro `easy.sudoku.puzzle.solver.free` ni cabeceras SQLite.

Conclusion operativa:
- en este entorno no fue posible extraer `Sudoku.db` desde Google Play Games por ADB ni por parseo offline del `userdata.img`.
