# Analisis incremental: Easy Sudoku (app referencia)

Fecha de inicio: 2026-02-25  
Fuente: decomp local en `artifacts/decomp/easy.sudoku.puzzle.solver.free_cli_20260224_215916`

Este documento se actualiza por bloques para no perder hallazgos por desconexion.

## 1) Identidad de la app (confirmado)

- Paquete: `easy.sudoku.puzzle.solver.free`  
  Evidencia: `resources/AndroidManifest.xml:6`
- Version: `5.48.0` (versionCode `441`)  
  Evidencia: `resources/AndroidManifest.xml:3-4`
- SDK: `minSdk 24`, `targetSdk 35`  
  Evidencia: `resources/AndroidManifest.xml:11-12`
- Activity de entrada: `com.meevii.ui.activity.SplashActivity`  
  Evidencia: `resources/AndroidManifest.xml:178`
- Deep links visibles: `oakevergames.onelink.me`, `classicsudoku.app`  
  Evidencia: `resources/AndroidManifest.xml:192`, `resources/AndroidManifest.xml:200`

## 2) Backend y red (confirmado)

### 2.1 Base URLs

- En build config quedan ofuscadas en bytes (`yb.h`):  
  Evidencia: `sources/yb/h.java:8`, `sources/yb/h.java:11`
- `yb.b.c()` usa `h.f120148a` como base principal.  
  Evidencia: `sources/yb/b.java:20-21`
- `xf.b.c()` tambien retorna `yb.h.f120148a`.  
  Evidencia: `sources/xf/b.java:37-38`
- Decodificacion (ejecutada localmente):
  - `https://api.freesudoku.me`
  - `https://matrix.dailyinnovation.biz`

### 2.2 API principal (Retrofit)

Interfaz `MeeviiApi`:

- `DELETE /sudoku/v1/user/me`  
- `POST /sudoku/v1/misc/signUploadRequest`
- `GET /sudoku/v1/user/gameData`
- `POST /sudoku/v1/user/awards`
- `POST /sudoku/v1/user/gameData`
- `GET /sudoku/v1/user/awards`

Evidencia: `sources/qf/a.java:16`, `:19`, `:22`, `:25`, `:28`, `:31`

### 2.3 Endpoints extra detectados

- `GET /sudoku/v1/activity?group=...`  
  Evidencia: `sources/jc/j.java:152`
- `POST /sudoku/v1/activity`  
  Evidencia: `sources/ic/t.java:281`, `sources/rf/n.java:359`

### 2.4 Firma y sincronizacion

En `SyncRepository`:

- Firma de request: hash de `payload + timestamp + "5ATWJxWZqP3ZHOPy9"`  
  Evidencia: `sources/rf/y0.java:58-60`
- `gameData` via JSON y firma+timestamp en API de usuario.  
  Evidencia: `sources/rf/y0.java:136`, `:151`

## 3) Headers / cliente HTTP (confirmado)

- Headers observados:
  - `User-Agent`
  - `today`
  - `app`
  - `version`
  - `versionNum`
  - `country`
  - `platform`
  - `language`
- Evidencia:
  - `sources/xf/b.java:34`
  - `sources/kc/i.java:119`

## 4) Persistencia local (confirmado)

### 4.1 Room DB principal

- DB file: `Sudoku.db` (muchas migraciones, v26)
  Evidencia: `sources/xf/j.java:525`, `sources/com/meevii/data/db/SudokuGameDataBase.java:17`
- Tablas:
  - `SudokuGame`
  - `Dc`
  - `active_medal`
  - `battle_season`
  - `tournament_season`
  - `favourite`
  Evidencia: `sources/com/meevii/data/db/SudokuGameDataBase_Impl.java:210-215`

### 4.2 DB de alarmas

- DB file: `alarmDb`, version 1
  Evidencia: `sources/com/meevii/data/db/SudokuAlarmDataBase.java:11`, `:22`
- Tabla: `alarm`
  Evidencia: `sources/com/meevii/data/db/SudokuAlarmDataBase_Impl.java:97`

### 4.3 SharedPreferences

- Pref principal: `easy.sudoku.puzzle.solver.free.v2.playerprefs`
  Evidencia: `sources/com/meevii/common/utils/k1.java:311`

### 4.4 JSON en filesDir

- `abyss_active_info.json`  
  Evidencia: `sources/ic/i.java:239`, `:502`, `:951`
- `tournament_season_info.json`  
  Evidencia: `sources/ic/t0.java:133`
- `last_tournament_season_info.json`  
  Evidencia: `sources/ic/t0.java:276`, `:592`

## 5) Modos / tipos / reglas (confirmado)

- `GameMode`: `BEGINNER, EASY, MEDIUM, HARD, EXPERT, SIXTEEN, EXTREME, SIX`  
  Evidencia: `sources/com/meevii/sudoku/GameMode.java:8-16`
- `GameType`: `NORMAL, DC, ACTIVE, BATTLE, DAILY, ZEN, TEACHING, STAGE, HARDEST`  
  Evidencia: `sources/com/meevii/sudoku/GameType.java:8-17`
- `SudokuType`: `NORMAL, ICE, KILLER, FUN`  
  Evidencia: `sources/com/meevii/sudoku/SudokuType.java:7-11`
- Reglas/tablero: 9x9, 6x6, 16x16 y variantes tiempo/errores/battle  
  Evidencia: `sources/com/meevii/sudoku/rules/GameRulesDescribe.java:6-16`

## 6) Banco de sudokus / generacion (confirmado)

### 6.1 Arquitectura de bancos

`QuestionBankConfig` enruta por tipo/modo a proveedores:

- `KILLER`, `SIXTEEN`, `DAILY`, `DYNAMIC`, `CONFRONTATION`, `RANDOM`, `RANK`, `EB_AND_JY`, `QB_LGN`, `DEFAULT`
- Evidencia: `sources/com/meevii/data/QuestionBankConfig.java:35-49`, `:174-184`

### 6.2 Archivos de banco (assets)

Hay gran cantidad de bancos `defaultQb*` en `resources/assets/config`:

- `defaultQb.json`, `defaultQb_lion_202412.json`, `defaultQb_lion_202501.json`, `defaultQb_lgn.json`, `defaultQbSolverDetails.json`, etc.
- Evidencia: listado de archivos en `resources/assets/config`

### 6.3 Cifrado/llave nativa

- Lectura de bancos y configs con `DecryptionUtils.nativeGetKey(...)`
- Evidencia:
  - `sources/ui/b.java:525`
  - `sources/lf/h.java:270`
  - `sources/lf/p.java:96`
  - `sources/lf/r.java:167`
  - `sources/com/learnings/decryption/DecryptionUtils.java:29`

Conclusion tecnica: el juego usa bancos preconstruidos + seleccion por capas/AB, no un generador puro "al vuelo" para cada partida (al menos en la ruta principal detectada).

## 7) Dificultad y desbloqueo (confirmado)

`GameModeLockManager` define victorias necesarias por modo previo:

- `MEDIUM`: 2
- `HARD`: 3
- `EXPERT`: 3 o 5 (segun grupos AB)
- `SIXTEEN/EXTREME`: 5 o 10 (segun grupos AB)

Evidencia: `sources/com/meevii/user/GameModeLockManager.java:84`, `:87`, `:93-97`

## 8) Puntuacion / skill rating (confirmado avanzado)

### 8.1 Selector de score final (confirmado)

- El juego mantiene dos score (`oldScore` y `newScore`) y el final sale por version:
  - `finalScore = newScore` si `scoreVersion == 1`
  - `finalScore = oldScore` en otro caso
- Evidencia:
  - `sources/com/meevii/data/bean/GameData.java:650-651`
  - campo `scoreVersion`: `sources/com/meevii/data/bean/GameData.java:276`

### 8.2 Condicion de "perfect" (confirmado)

- `isPerfect()` se define estrictamente como:
  - `hintUsedCount == 0 && totalMistake == 0`
- Evidencia:
  - `sources/com/meevii/data/bean/GameData.java:1161-1162`
  - campos: `hintUsedCount` (`:130`), `totalMistake` (`:306`)

### 8.3 Tabla de coeficientes por dificultad (confirmado)

En `SudokuScore.NewScoreInfo` (orden: `clearScore, errorScore, perfectScore, timeScore, fillTimeBaseScore, fillTimeRadio`):

- `SIX`: `(2, 5, 10, 5, 18, 1)`
- `EASY`: `(3, 10, 20, 10, 27, 1)`
- `BEGINNER`: `(3, 10, 20, 10, 27, 1)`
- `MEDIUM`: `(4, 15, 30, 15, 42, 2)`
- `HARD`: `(5, 20, 40, 20, 76, 3)`
- `EXPERT`: `(6, 25, 50, 25, 112, 4)`
- `EXTREME`: `(7, 30, 60, 30, 132, 4)`

Evidencia: `sources/com/meevii/sudoku/plugin/SudokuScore.java:35-42`

### 8.4 Flujo de suma de score por jugada y cierre (confirmado)

- En cada fill correcto editable:
  - suma bonus por "numero agotado" via `H(...)`
  - aplica ajuste base via `F(...)`
  - aplica bonus de fill-time y clears via `G(...)`
  - emite `ScoreChangeBean` con origen `FILL`
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:481-513`
  - `ScoreChangeFrom.FILL`: `sources/com/meevii/data/bean/ScoreChangeBean.java:24-27`
- Al terminar partida (hook `k()`):
  - aplica bonus ICE final `s(...)`
  - aplica bonus de cierre `r(...)` (perfect/mistakes/time)
- Evidencia: `sources/com/meevii/sudoku/plugin/SudokuScore.java:517-528`

### 8.5 Componentes del "new score" (confirmado)

- Bonus por fill-time:
  - `fillTimeBaseScore - fillTimeRadio * min(timeNoFillRight, 12)` (o 12 fijo en flujo rapido)
  - mas bonus por completar fila/columna/caja 3x3 (`clearScore` por cada una cerrada)
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:110-130`
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:256-337`
- Bonus de cierre por perfect + margen de errores + tiempo:
  - perfect: `+perfectScore` si `isPerfect()`
  - error: `+errorScore * max(0, limitMistake - mistake)`
  - tiempo: `+timeScore * v(time, scoreTimeMap)`
- Evidencia: `sources/com/meevii/sudoku/plugin/SudokuScore.java:147-182`
- Buckets de tiempo (`v`):
  - retorna `10, 8, 6, 4, 2` segun umbrales de `scoreTimeMap`
  - fallback `5` si mapa invalido
- Evidencia: `sources/com/meevii/sudoku/plugin/SudokuScore.java:236-247`

### 8.6 Bonus por "number use up" y fuentes de delta (confirmado)

- Si un digito se agota, puede sumar bonus de AB (`NumberClearScoreABTestHelper`), y emite `ScoreChangeFrom.NUMBER_USE_UP`.
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:133-145`
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:339-349`
  - `sources/com/meevii/data/bean/ScoreChangeBean.java:24-27`
- Mapa AB visible (parcial, algunos valores quedan via `R` de recursos):
  - `SIX=140`, `EASY=150`, `HARD=210`, `EXTREME=300`, `SIXTEEN=150`, `KILLER=210`, `ICE=<resource>`
- Evidencia: `sources/com/meevii/abtest/helper/NumberClearScoreABTestHelper.java:28-53`

### 8.7 Reglas AB para battle/explore (confirmado)

- Battle entra en score AB cuando `gameType == BATTLE`.
- Explore entra en score AB para `DAILY` con `KILLER` o `ICE`.
- Para recalcular score, `ICE` se trata como `EXPERT` y `KILLER` como `HARD`.
- Evidencia:
  - `sources/com/meevii/abtest/helper/BattleAddScoreAbtestHelper.java:41-43`
  - `sources/com/meevii/abtest/helper/ExploreAddScoreAbtestHelper.java:51-56`
  - `sources/com/meevii/abtest/helper/ExploreAddScoreAbtestHelper.java:44-49`
  - uso en filtro de tipos: `sources/com/meevii/sudoku/plugin/SudokuScore.java:99-101`

### 8.8 Ruta de "oldScore" (inferencia de alta confianza)

- `F(...)` aparece decompilado de forma incompleta (`UnsupportedOperationException`), pero el pseudocodigo mostrado permite inferir:
  - premio por acierto dependiente de tiempo y modo (`SIX`, `BEGINNER/EASY`, `MEDIUM`, `HARD`, `EXPERT`, `EXTREME`, `SIXTEEN`)
  - penalidad fija por modo en error (`-60`, `-150`, `-180`, `-400`, `-640`, `-840`)
  - suma final: `oldScore += delta_modo + extra` (extra viene de bonus ICE/otros)
- Evidencia base del pseudocodigo:
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:370-475`
  - llamada desde flujo principal: `sources/com/meevii/sudoku/plugin/SudokuScore.java:501`

## 9) Smart-hints y tecnica (confirmado)

- Archivo `defaultQbSolverDetails.json` se usa para resumen/analitica de solucion.
- Lista ordenada de tecnicas incluye:
  - `x_wing`, `swordfish`, `jellyfish`, `xy_wing`, `xy_chain`, etc.
- Evidencia: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:30`, `:145`, `:251`

## 10) Inventario util para balance (confirmado)

En `resources/assets/config`:

- `battle_name.json`: 32 grupos / 26229 nombres (conteo local)
- `dc_complete_user_num.json`: 1440 claves
- `dc_join_user_num.json`: 1440 claves
- `rank_active_question.json`: 105 entradas
- `question_time_map.json`: 4409 claves

## 11) Seguridad de red (confirmado)

- `cleartextTrafficPermitted=false` por defecto.
- Excepciones puntuales con cleartext en dominio-config (`127.0.0.1`, `69.234.247.27`, dominios de unityads).
- Evidencia: `resources/res/xml/network_security_config.xml:3`, `:8-31`

## 12) Contrato endpoint activity (confirmado)

### 12.1 Listado de actividades (GET)

- Endpoint:
  - `GET /sudoku/v1/activity?group=<ab_group>`
- Lectura de respuesta:
  - `data.list` (clave `list` via `AbCenterConstant.RULE_TYPE_LIST`)
- Evidencia:
  - `sources/jc/j.java:152`
  - `sources/jc/j.java:161-167`
  - `sources/com/learnings/abcenter/util/AbCenterConstant.java:19`

### 12.2 Solicitud de medallas por activity (POST)

- Endpoint:
  - `POST /sudoku/v1/activity`
- Request JSON:
  - `{ "activity_id_list": [<activityId>, ...] }`
- Response JSON usada por la app:
  - `data.activity_map."<activityId>".medal_icons` (array)
- Evidencia:
  - request body: `sources/rf/n.java:351-359`, `sources/ic/t.java:277-281`
  - response parse: `sources/rf/n.java:381-393`, `sources/ic/t.java:283-287`

### 12.3 Uso funcional de `medal_icons` (confirmado)

- La app usa indices de pagina para tomar el icono:
  - `medal_icons[pageIndex]`
- Luego actualiza registros locales de medallas activas.
- Evidencia:
  - `sources/rf/n.java:393-402`
  - `sources/ic/t.java:286-290`

### 12.4 Campos de actividad parseados en cliente (confirmado)

- En parse base de actividad aparecen, entre otros:
  - `id`, `type`, `release_start_at`, `release_end_at`, `will_start_at`, `theme`, `rel_activity_id`, `medal_icons`
- En flujo de completados se usan ademas:
  - `pageCount`, `pages[]`, `activeTitle`, `completedDate`, `medalIcon`
- Evidencia:
  - `sources/jc/d.java:157`, `:187-194`, `:200`
  - `sources/ic/t.java:240-243`, `:260-263`

## 13) Pendientes de extraccion (restantes)

- Cruzar todo esto contra nuestro backend objetivo (LAN/offline + sync) y dejar propuesta de esquema (eventos, idempotencia, reconciliacion).

## 14) Contratos DAO secundarios (confirmado)

### 14.1 Mapeo DAO por DB (Room)

- `SudokuGameDataBase` expone:
  - `e` (BattleSeasonDao), `m` (SudokuGameDao), `of.g` (DcDao), `i` (FavouriteDao), `of.a` (ActiveMedalDao), `o` (TournamentSeasonDao)
- Evidencia:
  - `sources/com/meevii/data/db/SudokuGameDataBase.java:19-29`
  - `sources/com/meevii/data/db/SudokuGameDataBase_Impl.java:67`, `:82`, `:97`, `:144`, `:159`, `:174`

### 14.2 Esquema de tablas secundarias

- `Dc`:
  - `id`, `level`, `mode`, `sudokuType`, `state`, `date`, `taskLevel`, `isTaskComplete`, `layer`
- `active_medal`:
  - `id`, `activeId`, `pageCount`, `page`, `theme`, `type`, `activeTitle`, `completedDate`, `medalIcon`, `medalLevel`, `medalFrame`, `medalFrameType`, `bestRanking`
- `battle_season`:
  - `id`, `level`, `lastLevel`, `star`, `lastStar`, `winCount`, `failCount`, `winContinuedCount`, `seasonStartTime`, `seasonEndTime`
- `tournament_season`:
  - `id`, `seasonIndex`, `userRank`, `userScore`, `seasonStartTime`, `seasonEndTime`, `updateTime`, `tierId`
- `favourite`:
  - `id`, `question`, `updateTime`, `mode`, `status`
- Evidencia:
  - `sources/com/meevii/data/db/SudokuGameDataBase_Impl.java:211-215`

### 14.3 `ActiveMedalDao` (`of.a`)

- Lectura:
  - por `activeId`
  - por `activeId + page`
  - listado total
  - filtro especial: `activeId` entre `6201` y `62015`
- Escritura:
  - insert/upsert uno y batch
  - update puntual de `medalIcon` por `activeId + page`
  - delete all
- Evidencia:
  - `sources/of/a.java:12-33`
  - entidad `active_medal`: `sources/pf/a.java:9`

### 14.4 `DcDao` (`of.g`)

- Lectura:
  - lista completa `order by id asc`
  - por `date`
  - `LiveData` de completados (`state = 15`)
  - agregado: `count(date)` agrupado por fecha para completados
- Escritura:
  - insert/upsert (single y batch)
  - delete all
  - impl incluye statement de normalizacion por fecha: `update dc set state = 15 where date = ?`
- Evidencia:
  - `sources/of/g.java:13-34`
  - `sources/of/h.java:89`
  - entidad `Dc`: `sources/pf/c.java:13`

### 14.5 `BattleSeasonDao` (`of.e`)

- Lectura:
  - lista ordenada por `seasonStartTime desc`
  - por `id`
  - ultimo season distinto al actual (`id != :currentId`, `limit 1`)
- Escritura:
  - insert/upsert single y batch
  - delete all
- Evidencia:
  - `sources/of/e.java:13-28`
  - entidad: `sources/com/meevii/data/db/entities/BattleSeasonEntity.java:10`, `:12`

### 14.6 `TournamentSeasonDao` (`of.o`)

- Lectura:
  - lista `seasonStartTime desc` y asc
  - por `seasonIndex`
  - historico filtrado por `seasonEndTime < :beforeTime` con variantes:
    - top10 (`userRank < 10`) asc/desc
    - con score (`userScore > 0`) desc
- Escritura:
  - insert/upsert single
- Evidencia:
  - `sources/of/o.java:12-30`
  - entidad `tournament_season`: `sources/pf/g.java:10`, `:21`, `:26`, `:31`, `:46`, `:51`

### 14.7 `FavouriteDao` (`of.i`)

- Lectura:
  - `LiveData` ordenado por `updateTime desc`
  - snapshot top 100 (`limit 100`)
- Escritura/mantenimiento:
  - insert/upsert
  - delete por `question`
  - pruning para mantener max 100 (`DELETE ... NOT IN (SELECT ... LIMIT 100)`)
- Evidencia:
  - `sources/of/i.java:13-25`
  - entidad `favourite`: `sources/pf/d.java:11`, `:22`, `:27`, `:37`

## 15) Dificultad desde solver details (confirmado + propuesta)

### 15.1 Estado real del archivo `defaultQbSolverDetails.json`

- Confirmado: archivo descifrado completo fuera de runtime Android.
- Flujo confirmado:
  - `nativeGetKey(...)` obtiene key AES en nativo (`libmeevii_native.so`),
  - `h9.a.a(...)` hace AES/CBC/PKCS5Padding con IV en cero,
  - resultado parsea a `Map<String, Map<String, JsonElement>>`.
- Artefacto generado:
  - `resources/assets/config/defaultQbSolverDetails.decrypted.json` (9369 puzzles, JSON valido).
- Key AES recuperada del flujo nativo/firma APK:
  - `SKpvYaOKKIz+2dpO`
- Evidencia:
  - carga archivo: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:145`
  - parse JSON map: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:149`
  - decrypt call: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:249-251`
  - key nativa + lib: `sources/com/learnings/decryption/DecryptionUtils.java:19`, `:29`
  - algoritmo AES/IV: `sources/h9/a.java:39-41`
  - `DEFAULT_ALGORITHM`: `sources/io/appmetrica/analytics/coreutils/internal/encryption/AESEncrypter.java:13`

### 15.2 Modelo de datos usado por la app

- Por puzzle (`qid` como string), el valor es un mapa:
  - clave: tecnica de resolucion
  - valor: cantidad de celdas/pasos de esa tecnica
- `getQuestionSolutionInfoList` crea `SolutionInfo(solutionName, cellNum, difficultValue, SERating)`:
  - `difficultValue = index de la tecnica en sortedSolverKeys`
  - `SERating = rate de technique` (string)
- Evidencia:
  - lista ordenada de tecnicas: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:30`
  - construccion `SolutionInfo`: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:161-172`
  - orden por `cellNum` y por `difficultValue`: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:258-272`

### 15.3 Pesos/tasas por tecnica (SE rate)

- Tabla base de rates (extraida de `solutionRateMapping`):
  - bajos: `last_free_cell=1`, `last_possible_number=1.1`, `hidden_single=1.5`
  - intermedios: `locked_candidates_claiming=2.5`, `locked_candidates_pointing=2.6`, `x_wing=3.2`, `hidden_pair=3.4`, `swordfish=3.8`
  - altos: `xy_wing=4.2`, `xyz_wing=4.4`, `uniqueness=4.5`, `hidden_quadruple=5.4`, `finned_jellyfish=5.8`, `xy_chain=7.5`
- Nota: `hidden_triple`, `skyscraper`, `w_wing`, `turbot_fish` usan `Protocol.VAST_1_0_WRAPPER` (`"4"`).
- Evidencia:
  - rates: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:108-140`
  - constante `"4"`: `sources/net/pubnative/lite/sdk/models/Protocol.java:6`

### 15.4 Calibracion real sobre dataset completo (cerrado)

Definicion por puzzle:
- `weighted_se = sum( count(technique) * rate(technique) )`
- `max_rate = max( rate(technique) con count > 0 )`
- `advanced_hits = sum(count) de tecnicas con rate >= 4.5`

Resultados sobre `9369` puzzles:
- `weighted_se`: min `40.8`, max `197.6`, media `72.16`
- percentiles `weighted_se`: p20 `46.4`, p40 `55.1`, p60 `74.4`, p80 `96.8`, p95 `122.4`, p99 `143.2`
- `advanced_hits`: p95 `0`, p99 `1`
- distribucion `max_rate` (top): `1.1(2934)`, `2.0(2884)`, `2.6(1031)`, `3.0(676)`, `4.0(612)`, `4.2(386)`, `7.5(148)`

Umbrales calibrados recomendados (6 tiers):
- `Beginner`: `max_rate <= 2.0` y `weighted_se < 46.4`
- `Easy`: `max_rate <= 2.6` y `weighted_se < 55.1`
- `Medium`: `max_rate <= 3.4` y `weighted_se < 74.4`
- `Hard`: `max_rate <= 4.2` y `weighted_se < 96.8`
- `Expert`: `max_rate <= 5.8` y `weighted_se < 122.4`
- `Extreme`: resto, o `xy_chain` presente, o `advanced_hits >= 2`

Distribucion resultante con esos umbrales (dataset referencia):
- `Beginner 19.34%`, `Easy 20.61%`, `Medium 19.84%`, `Hard 19.50%`, `Expert 14.99%`, `Extreme 5.72%`

Razon tecnica:
- Respeta el ranking relativo de tecnicas (`sortedSolverKeys` + `SE rate`) y evita sesgo extremo hacia `Beginner`, dejando una distribucion util para progresion.
- Para calibraciones futuras (pesos/umbrales/time-map), usar el proceso versionado en `docs/SudokuArena/Dataset-Calibracion-Playbook.md`.

## 16) Mapa de datos de puzzle (confirmado)

### 16.1 Codificacion del tablero (`question`)

- El `question` base es una cadena compacta de celdas:
  - `9x9`: longitud `81`
  - `6x6`: longitud `36`
  - `16x16`: longitud `256`
- Semantica por caracter:
  - `A..I` (o `A..F`, `A..P`): celda `given` (no editable)
  - `a..i` (o `a..f`, `a..p`): celda editable
  - valor numerico: `answerNum = (char - 'A'/'a') + 1`
- Evidencia:
  - parse de mayus/minus a `canEdit + answerNum`:
    - `sources/com/meevii/history/HistorySudokuView.java:145-154`
    - `sources/com/meevii/guide/view/GuideSudokuView.java:335-347`

### 16.2 Formatos de bancos `defaultQb*`

- Todos los bancos se descifran con el mismo flujo AES nativo (`nativeGetKey` + AES/CBC/PKCS5 + IV cero).
- `defaultQb.json`, `defaultQb_lion_*.json`, `defaultQb_lgn*.json`:
  - estructura: `{"<layer>": ["<question>_<index>_<winRate>", ...], ...}`
  - en algunos casos `question` viene como `"<question>;<killerGroup>"`.
  - parser:
    - split por `_` y parse de `winRate`: `sources/lf/k.java:39-53`, `sources/lf/l.java:29-37`
    - split por `;` para extraer `killerGroup`: `sources/lf/k.java:151-156`
- `defaultQbRandom*.json`:
  - estructura: `{"easy":[ "<question>", ...], ...}`
  - parser crea `QuestionConfigBean(question, id, winRate=0)`:
    - `sources/lf/r.java:101-103`
- `defaultQbRank*.json`:
  - estructura: `{"easy":[ "<question>_<rank>", ...], ...}`
  - parser usa `rank = split[1]`:
    - `sources/lf/o.java:281-291`
- `defaultQbConfrontation.json`:
  - estructura observada: `{"easy":[ "<question>_<x>_<winRate>", ...], ...}`
  - parser toma `winRate = split[2]`:
    - `sources/lf/h.java:173-181`
- `defaultQbEBAndJY.json`:
  - estructura: `{"easy":[ "<question>", ...], ...}`
  - parser usa indice de array para construir id:
    - `sources/lf/p.java:24-26`

### 16.3 Que representa cada dataset auxiliar

- `defaultQbSolverDetails.json` (ya descifrado):
  - `Map<qid, Map<technique, count>>`
  - no guarda pasos de hint, guarda conteo agregado por tecnica.
  - evidencia parse: `sources/com/meevii/abtest/helper/SuccessSolutionSummaryAbTestHelper.java:149`
- `question_time_map.json`:
  - `Map<qid, [t1,t2,t3,t4]>` (4 umbrales de tiempo por puzzle)
  - usado por score de tiempo (`10/8/6/4/2`): `sources/com/meevii/sudoku/plugin/SudokuScore.java:236-247`
- `rank_active_question.json`:
  - lista de objetos `{difficulty, level, reward}` para recompensas/rank activity.

### 16.4 Id de partida vs id de puzzle

- El `questionId` de runtime no es estable global:
  - se arma como `mode + randomDigit + paddedIndex`.
  - evidencia: `sources/com/meevii/data/a.java:14-29`
- El identificador estable de puzzle es el `question` string (qid logico), que es la clave de `question_time_map` y `solverDetails`.

### 16.5 Implicacion para SudokuArena (diseno propio recomendado)

- Si generamos dataset propio, necesitamos al menos:
  - `question` (canonico, estable),
  - `difficulty metrics` (`weighted_se`, `max_rate`, `advanced_hits`),
  - `time thresholds` por puzzle (equivalente a `question_time_map`),
  - opcional: `solverDetails` por tecnica para analytics y UI de resumen.
- Las pistas "paso a paso" no estan preguardadas en estos JSON; se derivan en runtime por motor de hints/solver.

## 17) Extracto funcional solicitado (cronometro, errores, config, colores, perfil, storage, sync)

### 17.1 Cronometro y tiempo de partida (confirmado)

- `SudokuTime` corre tick cada segundo (`postDelayed(..., 1000L)`) y actualiza:
  - `time`
  - `timePeriod`
  - `timeNoFill`
  - `timeNoFillRight`
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuTime.java:63`
  - `sources/com/meevii/sudoku/plugin/SudokuTime.java:76-79`
- Soporta estados `BEGIN/CONTINUE/PAUSE/END` y pausa/reanuda desde control.
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuTime.java:38`
  - `sources/com/meevii/sudoku/SudokuControl.java:630`
- En modo `TIME_AND_MISTAKE_LIMIT_9_9` extiende limite de tiempo en +120s al alcanzar el limite.
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuTime.java:205-206`

### 17.2 Errores/mistakes (confirmado)

- Estado de errores en runtime (`GameData`):
  - `limitMistake`
  - `mistake`
  - `totalMistake`
  - `mistakeCellState`
  - `scoreTimeMap`
- Evidencia:
  - `sources/com/meevii/data/bean/GameData.java:200`
  - `sources/com/meevii/data/bean/GameData.java:209`
  - `sources/com/meevii/data/bean/GameData.java:273`
  - `sources/com/meevii/data/bean/GameData.java:925`
- Persistencia de mistakes al guardar/reanudar:
  - `setMistake(...)`, `setTotalMistake(...)`, `setMistakeCellStateString(...)`.
- Evidencia:
  - `sources/com/meevii/sudoku/SudokuControl.java:833-834`
  - `sources/com/meevii/sudoku/SudokuControl.java:1043-1044`
  - `sources/com/meevii/sudoku/SudokuControl.java:1073`
- Impacto en score:
  - `errorScore * max(0, limitMistake - mistake)` en cierre.
- Evidencia:
  - `sources/com/meevii/sudoku/plugin/SudokuScore.java:160-161`

### 17.3 Configuraciones de usuario (confirmado)

- Toggles visibles en settings (persistidos por key):
  - `key_mistakes_limit`
  - `key_number_first`
  - `key_light_mode`
  - `key_highlight_areas`
  - `key_highlight_identical_numbers`
  - `key_smart_hint_enable`
  - `key_sound_effect`
  - `key_vibration`
- Evidencia:
  - `sources/com/meevii/ui/activity/SettingActivity.java:332`
  - `sources/com/meevii/ui/activity/SettingActivity.java:712`
  - `sources/com/meevii/ui/activity/SettingActivity.java:772`
  - `sources/com/meevii/ui/activity/SettingActivity.java:778`
  - `sources/com/meevii/ui/activity/SettingActivity.java:784`
  - `sources/com/meevii/ui/activity/SettingActivity.java:796`
  - `sources/com/meevii/ui/activity/SettingActivity.java:826`
  - `sources/com/meevii/ui/activity/SettingActivity.java:832`

### 17.4 Colores y tema del tablero (confirmado)

- El tablero expone tokens de tema dedicados:
  - `chessboardBgSelectColor`
  - `chessboardBgSelectSameColor`
  - `chessboardBgSelectWeakColor`
  - `chessboardBgStrongColor`
  - `chessboardBgErrorColor`
  - `chessboardFgErrorColor`
- Evidencia:
  - `resources/res/values/attrs.xml:814-836`
- Hay multiple presets de colores en estilos (dark/light y themes adicionales), no solo un set fijo.
- Evidencia:
  - `resources/res/values/styles.xml:4447-4458`
  - `resources/res/values/styles.xml:4717-4728`
  - `resources/res/values/styles.xml:5527-5538`

### 17.5 Perfil de usuario (confirmado)

- Flags de uso/edicion de perfil:
  - `key_sp_can_edit_user_profile_date`
  - `key_sp_use_user_profile`
- Evidencia:
  - `sources/ic/t0.java:1158-1159`
- Perfil competitivo (battle):
  - key para nombre de usuario: `key_battle_user_name`.
- Evidencia:
  - `sources/ic/y0.java:40`
  - `sources/ic/y0.java:54`
  - `sources/ic/y0.java:62`

### 17.6 Informacion guardada localmente (confirmado)

- SharedPreferences principal:
  - `easy.sudoku.puzzle.solver.free.v2.playerprefs`
- Evidencia:
  - `sources/com/meevii/common/utils/k1.java:311`
- DB principal local:
  - `Sudoku.db` con migraciones acumuladas.
- Evidencia:
  - `sources/xf/j.java:525`
- Tablas auxiliares adicionales detectadas:
  - `battle_season`, `active_medal`, `tournament_season`, `favourite`.
- Evidencia:
  - `sources/xf/j.java:157`
  - `sources/xf/j.java:185`
  - `sources/xf/j.java:389`
  - `sources/xf/j.java:358`
- Cache JSON en `filesDir`:
  - `abyss_active_info.json`
  - `tournament_season_info.json`
  - `last_tournament_season_info.json`
- Evidencia:
  - `sources/ic/i.java:239`, `:502`, `:951`
  - `sources/ic/t0.java:133`, `:276`, `:592`

### 17.7 Sincronizacion online (confirmado)

- Endpoint de sincronizacion de progreso:
  - `GET /sudoku/v1/user/gameData`
  - `POST /sudoku/v1/user/gameData`
- Evidencia:
  - `sources/qf/a.java:22`
  - `sources/qf/a.java:28`
- Flujo de sync repository:
  - descarga zip remoto `syncRemoteData.zip`
  - descomprime a `syncData`
  - subida con payload firmado y `gameData` serializado
  - solicitud previa de upload firmado con `contentType` y `md5`
- Evidencia:
  - `sources/rf/y0.java:69`
  - `sources/rf/y0.java:89`
  - `sources/rf/y0.java:142-143`
  - `sources/rf/y0.java:151`

### 17.8 Implicacion directa para SudokuArena (recomendacion tecnica)

- Para no perder paridad funcional minima en nuestro roadmap online:
  - separar claramente `tiempo total` vs `tiempo entre jugadas` (equivalente a `timeNoFill/timeNoFillRight`),
  - persistir `mistake` y `totalMistake` por partida,
  - definir un catalogo de settings con keys estables (UI + domain + persistencia),
  - usar tokens de color del tablero por tema (no hardcode),
  - separar almacenamiento local en 3 capas: preferencias, base de partidas, caches efimeros,
  - diseñar sync incremental de `gameData` + flujo de archivo snapshot para recuperacion.

## 18) Backlog derivado para tema/resaltado/animaciones

Para implementacion en SudokuArena, el backlog operativo quedo consolidado en:

- `docs/SudokuArena/Registro-Avance-Ideas.md` -> Feature: `UI Theme + Resaltado + Animaciones de Completado`

Cobertura del bloque:

- gestion de tema `System/Light/Dark`,
- paleta semantica propia y tokens de estado visual,
- reglas de prioridad de resaltado (conflicto/seleccion/coincidencia/relacionado),
- animacion de completado para fila/columna/3x3 y combinaciones,
- toggles de configuracion y pruebas de regresion.
