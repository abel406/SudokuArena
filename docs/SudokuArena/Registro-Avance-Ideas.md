# Registro de Avance e Ideas (SudokuArena)

Este documento sirve como registro vivo de:
- lo implementado,
- lo parcialmente implementado,
- lo pendiente,
- y nuevas ideas para priorizar.

Actualizado: 2026-02-25

Leyenda de siglas del proyecto: `docs/SudokuArena/Leyenda-Siglas.md`

## Hecho
- Arquitectura base por capas (`Domain`, `Application`, `Infrastructure`, `Server`, `Desktop`, `Sync.Worker`).
- Script de arranque local con restore/build/test y lanzamiento de server + desktop.
- Juego Sudoku local en WPF con:
  - tablero interactivo,
  - seleccion de numero (1..9),
  - comando `Deshacer` base (pendiente de cierre final de contrato y pruebas),
  - modo `Borrar` estable por click en celda editable (ON/OFF), protegido por estado `given/editable`,
  - resaltado visual ajustado: gris claro para fila/columna/3x3 y azul para celda activa + digitos coincidentes,
  - conteo de errores con limite,
  - puntaje,
  - reloj,
  - dialogos de victoria/derrota,
  - pruebas Desktop para flujo de borrado (incluyendo escenario general de celdas no-given en tablero casi completo).
- Pipeline Desktop de publicacion:
  - `dotnet publish` ahora ejecuta generacion + validacion automatica del dataset runtime (`tools/puzzle_dataset/generate_runtime_dataset.ps1` + `validate_runtime_dataset.ps1`),
  - cantidad configurable por build con `PuzzleDatasetTargetTotal` (default: `9369`),
  - umbral minimo obligatorio configurable con `PuzzleDatasetMinTotal` (default: `9369`), si no se alcanza el build/publish falla.
- Fuentes de puzzles separadas en Desktop:
  - `ServerSeedPuzzleProvider` (fuente servidor/seed remoto, opcional),
  - `LocalSeedPuzzleProvider` (seed local embebido),
  - `CompositePuzzleProvider` con prioridad `server -> local` para fallback controlado.
- Contrato unificado de puzzle preparado para futuros modos/tableros:
  - `PuzzleBoardKind` (`Classic9x9`, `SixBySix`, `SixteenBySixteen`) y `PuzzleMode` (`Beginner..Extreme`, `Six`, `Sixteen`) agregados al schema/runtime,
  - `PuzzleModeResolver` para inferencia consistente cuando el dataset no especifique modo,
  - `JsonPuzzleProvider` Desktop mantiene soporte activo en `Classic9x9` y omite otros `board_kind` hasta implementar controles 6x6/16x16.
- Backend base con:
  - creacion y consulta de partidas,
  - hub SignalR para movimientos,
  - persistencia local en SQLite.
- API de administracion para themes y media (fase 1) con control temporal por header de rol.
- Worker de outbox para envio de eventos hacia servicio cloud.

## Parcial
- Analisis tecnico incremental de app de referencia (Easy Sudoku) documentado en `docs/SudokuArena/Analisis-Referencia-EasySudoku.md`:
  - ya extraidos contrato `GET/POST /sudoku/v1/activity` (request/response y campos parseados),
  - scoring avanzado (selector old/new score, coeficientes por modo, bonus por tiempo/error/perfect/fill, AB hooks battle/explore),
  - contratos DAO secundarios ya mapeados (`Dc`, `active_medal`, `battle_season`, `tournament_season`, `favourite`),
  - `defaultQbSolverDetails.json` ya descifrado y analizado completo (9369 puzzles),
  - extracto funcional consolidado (cronometro, errores, configuraciones, colores, perfil, persistencia local y sincronizacion) en seccion 17 del analisis,
  - mapa de formatos de bancos (`defaultQb*`, `question_time_map`, `rank_active_question`) ya documentado para diseno de dataset propio,
  - escala de dificultad calibrada con percentiles reales (`weighted_se`, `max_rate`, `advanced_hits`) y umbrales recomendados de 6 tiers,
  - pendiente aterrizar propuesta final para backend LAN/offline+sync.
- Matriz tecnica de implementacion creada en `docs/SudokuArena/Matriz-Tecnica-Referencia-SudokuArena.md` (feature -> dato -> fuente -> implementacion en SudokuArena), lista para convertir filas en tareas.
- Desktop con modos LAN/Cloud: existe UI y comandos, falta conexion real completa a hub/API en flujo de partida.
- Perfil de jugador: modelo base creado (`PlayerProfile`), sin casos de uso/API/UI persistidos de punta a punta.
- Roles: existe gate temporal por `X-Role`, falta autenticacion/autorizacion real.
- Autocompletado: existe toggle en UI, falta logica funcional (backlog `AC-01..AC-06`).

## Pendiente
- Definir contrato funcional de `Deshacer`:
  - registrar historial completo de jugadas de usuario en orden cronologico,
  - deshacer en orden LIFO (ultima jugada, luego la anterior, etc.) hasta vaciar historial,
  - incluir alta/modificacion/borrado de celdas de usuario como jugadas deshacibles,
  - no permitir afectar celdas originales del puzzle,
  - cubrir con pruebas unitarias de historial y limites.
- Torneos completos: creacion, registro, bracket, emparejamiento, avance y cierre.
- Sistema ELO/ranking, niveles, estrellas y escudos.
- Login real con Google (Gmail) + emision y validacion de JWT.
- Gestion completa de usuarios y roles (admin/moderador/jugador).
- Perfil visible en duelo (pais/bandera/avatar) y barra de progreso de ambos jugadores.
- Modo "jugar contra la PC" para pruebas cuando no hay segundo jugador humano.
- Estadisticas por jugador e historial detallado de partidas.
- Sincronizacion cloud completa (incluyendo endpoint receptor y reconciliacion).
- Instaladores por perfil (LAN-only y Full-cloud).
- Cliente web y cliente movil (objetivo multiplataforma).
- Mas cobertura de pruebas: integracion, SignalR, UI y E2E.

## Ideas Nuevas (Backlog Vivo)
Regla: agregar nuevas ideas aqui antes de implementar, con prioridad y criterio de aceptacion.

### Feature: Dataset y Solver Propio (`tools/puzzle_dataset`)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| DS-01 | Generador de puzzles propio con unicidad garantizada | Alta | Planificada | Salida minima: genera `N` puzzles 9x9 validos y prueba automatica confirma solucion unica por puzzle. |
| DS-02 | Catalogo de tecnicas del solver (clase propia + i18n + pesos) | Alta | Planificada | Salida minima: `TechniqueId`/catalogo versionado con textos propios y pesos configurables sin dependencias de terceros. |
| DS-03 | Solver instrumentado para metricas de dificultad | Alta | Planificada | Salida minima: por puzzle exporta `weighted_se`, `max_rate`, `advanced_hits` y conteo por tecnica. |
| DS-04 | Calibrador automatico de pesos/umbrales | Alta | Propuesta | Salida minima: optimiza pesos con restricciones (monotonia/suavidad), genera tabla `v1` y reporte de distribucion/correlacion para validacion. Seguir `docs/SudokuArena/Dataset-Calibracion-Playbook.md`. |
| DS-05 | Estimador de umbrales de tiempo por puzzle | Media | Propuesta | Salida minima: genera `time_map[qid]=[t1,t2,t3,t4]` con metodo reproducible y validacion estadistica basica. Seguir `docs/SudokuArena/Dataset-Calibracion-Playbook.md`. |
| DS-06 | Exportador de dataset propio unificado (JSON unico con `question_bank` + `solver_details` + `time_map`) | Alta | Planificada | Salida minima: genera `puzzle_dataset.v1.json` con esquema versionado y validado por tests. |

Orden recomendado de ejecucion del feature:
1. `DS-01`
2. `DS-02`
3. `DS-03`
4. `DS-04`
5. `DS-05`
6. `DS-06`

### Feature: UI Theme + Resaltado + Animaciones de Completado

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| UI-01 | Auditar flujo de tema activo (System/Light/Dark) y fallback en Desktop | Alta | Completada | Implementado: `ThemeMode`, detector sistema, `ThemeManager`, selector UI y persistencia local de preferencia (`desktop-settings.json`). |
| UI-02 | Definir paleta semantica propia de tablero (`BoardThemePalette`) | Alta | Completada | Implementado: paleta semantica + resource keys + consumo en `SudokuBoardControl` + ajuste de tokens/contraste para Light/Dark. |
| UI-03 | Crear recursos para temas claro/oscuro con `DynamicResource` | Alta | Planificada | Salida minima: `ResourceDictionary` Light y Dark conectados a un selector de tema sin reiniciar app. |
| UI-04 | Normalizar reglas de prioridad de resaltado | Alta | Planificada | Salida minima: precedencia unica (`Conflict > Active > MatchingDigit > RelatedGroup > Normal`) documentada y cubierta por tests. |
| UI-05 | Ajustar colores de resaltado segun criterio del proyecto | Alta | Planificada | Salida minima: fila/columna/3x3 en gris claro; solo celda activa y digitos coincidentes en azul. |
| UI-06 | Implementar animacion de completado por fila/columna/3x3 | Alta | Completada | Implementado: evento de completado en VM + disparo de onda radial por unidad completada. |
| UI-07 | Soportar animacion combinada (fila+columna, fila+3x3, etc.) | Alta | Completada | Implementado: union de celdas objetivo por planner y animacion unica sin duplicados visibles. |
| UI-13 | Animacion global al completar partida (tablero completo) | Alta | Completada | Implementado: onda global desde ultima celda jugada y apertura de dialogo tras breve espera. |
| UI-08 | Agregar setting `CompletionAnimation` y respetar preferencia | Media | Planificada | Salida minima: toggle ON/OFF persistido y aplicado en runtime. |
| UI-09 | Agregar setting `ThemeMode` (`System/Light/Dark`) | Media | Planificada | Salida minima: persistencia local y aplicacion inmediata al cambiar. |
| UI-10 | Pruebas unitarias de resaltado por estado y prioridad | Alta | Planificada | Salida minima: suite de tests para casos de seleccion, coincidencia, conflicto y combinaciones. |
| UI-11 | Pruebas de integracion de tema claro/oscuro y contraste | Media | Propuesta | Salida minima: validacion automatizada de colores esperados por estado en ambos temas. |
| UI-12 | Pruebas de animacion de completado (incluyendo combinadas) | Media | Propuesta | Salida minima: verificacion de disparo correcto de flags `rowDone/colDone/boxDone` y rendering estable. |

Orden recomendado de ejecucion del feature:
1. `UI-01`
2. `UI-02`
3. `UI-03`
4. `UI-04`
5. `UI-05`
6. `UI-06`
7. `UI-07`
8. `UI-13`
9. `UI-08`
10. `UI-09`
11. `UI-10`
12. `UI-11`
13. `UI-12`

Agrupacion por fase:
1. Fase A - Fundacion de tema y paleta: `UI-01`, `UI-02`, `UI-03`.
2. Fase B - Reglas visuales de resaltado: `UI-04`, `UI-05`.
3. Fase C - Animaciones de completado: `UI-06`, `UI-07`, `UI-13`, `UI-08`.
4. Fase D - Configuracion de experiencia: `UI-09`.
5. Fase E - Calidad y regresion: `UI-10`, `UI-11`, `UI-12`.

### Feature: Autocompletado Asistido (Runtime + Settings)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| AC-01 | Definir contrato funcional de autocompletado | Alta | Planificada | Salida minima: reglas documentadas de activacion, exclusiones y comportamiento esperado en juego normal. |
| AC-02 | Implementar evaluador de oportunidad de autocompletado | Alta | Planificada | Salida minima: motor en Application/Desktop que detecta oportunidad valida (sin afectar givens ni jugadas invalidas). |
| AC-03 | Integrar accion de autocompletado en flujo de jugada | Alta | Planificada | Salida minima: accion aplica relleno asistido en celda editable, actualiza estado de tablero y conserva compatibilidad con deshacer/puntaje. |
| AC-04 | Conectar y respetar settings relacionados (`AutoComplete`, `AutoRemoveNotes`, `AutoNextNumber`) | Alta | Planificada | Salida minima: preferencia persistida y aplicada en runtime; comportamiento consistente entre toggles combinados. |
| AC-05 | Agregar telemetria local de uso de autocompletado | Media | Propuesta | Salida minima: contadores de uso local (clicks/rachas) y eventos internos para diagnostico. |
| AC-06 | Pruebas del flujo de autocompletado (unitarias + integracion VM) | Alta | Planificada | Salida minima: tests para casos habilitado/deshabilitado, sin oportunidad, con oportunidad y combinacion de settings. |

Orden recomendado de ejecucion del feature:
1. `AC-01`
2. `AC-02`
3. `AC-03`
4. `AC-04`
5. `AC-06`
6. `AC-05`

Agrupacion por fase:
1. Fase A - Contrato y reglas: `AC-01`.
2. Fase B - Motor de decision: `AC-02`.
3. Fase C - Integracion de jugada y settings: `AC-03`, `AC-04`.
4. Fase D - Calidad y observabilidad: `AC-06`, `AC-05`.

Subtareas tecnicas iniciales (UI-01/UI-02):

- `UI-01.1` `src/SudokuArena.Desktop/Theming/ThemeMode.cs`: enum `System/Light/Dark`.
- `UI-01.2` `src/SudokuArena.Desktop/Theming/WindowsThemeDetector.cs`: resolver tema del sistema + fallback robusto a Light.
- `UI-01.3` `src/SudokuArena.Desktop/Theming/ThemeManager.cs`: aplicar tema efectivo en runtime (`System` -> resolver claro/oscuro).
- `UI-01.4` `src/SudokuArena.Desktop/App.xaml` y `App.xaml.cs`: migrar a `ResourceDictionary` fusionados y aplicar tema inicial en arranque.
- `UI-01.5` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: exponer `ThemeMode` y comando para cambiarlo.
- `UI-01.6` `src/SudokuArena.Desktop/MainWindow.xaml`: agregar selector de tema (`System/Claro/Oscuro`) enlazado al VM.
- `UI-01.7` `test/SudokuArena.Desktop.Tests/ThemeManagerTests.cs`: tests de fallback y seleccion de tema efectivo.

- `UI-02.1` `src/SudokuArena.Desktop/Theming/BoardThemePalette.cs`: clase de paleta semantica (active, matching, related, conflict, lineas, textos).
- `UI-02.2` `src/SudokuArena.Desktop/Theming/ThemeResourceKeys.cs`: claves unificadas para evitar strings duplicados.
- `UI-02.3` `src/SudokuArena.Desktop/Themes/Theme.Light.xaml`: tokens y brushes de paleta Light.
- `UI-02.4` `src/SudokuArena.Desktop/Themes/Theme.Dark.xaml`: tokens y brushes de paleta Dark.
- `UI-02.5` `src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs`: reemplazar colores hardcodeados por lectura de brushes semanticos.

Plan de ejecucion previo (UI-06/UI-07/UI-08):

1. Fase 1 - Evento de completado desde ViewModel:
- `UI-06.1` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: emitir evento al aplicar jugada valida con flags `rowDone`, `colDone`, `boxDone`.
- `UI-06.2` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: no disparar evento en jugadas invalidas, celdas given, ni borrado sin efecto.
- Criterio de aceptacion: al completar fila/columna/cuadro, el evento se emite una vez por jugada con flags correctos.

2. Fase 2 - Motor de animacion en tablero:
- `UI-07.1` `src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs`: agregar estado temporal de animacion por celda y scheduler por anillos.
- `UI-07.2` `src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs`: construir conjunto objetivo por union de fila/columna/3x3 sin duplicados.
- `UI-07.3` `src/SudokuArena.Desktop/MainWindow.xaml.cs`: suscribir evento VM -> Board para iniciar animacion en celda origen.
- Criterio de aceptacion: si se cumplen varias condiciones a la vez (ej. fila+3x3), se anima union en un solo ciclo, sin flicker.

3. Fase 3 - Configuracion y pruebas:
- `UI-08.1` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: toggle `CompletionAnimation` persistido junto a settings locales.
- `UI-08.2` `src/SudokuArena.Desktop/Themes/Theme.Light.xaml` y `Theme.Dark.xaml`: tokens de color/alpha para pulso de completado.
- `UI-08.3` `test/SudokuArena.Desktop.Tests/*`: tests de emision de evento y builder de celdas animadas (row/col/box/combinadas).
- Criterio de aceptacion: `CompletionAnimation=OFF` desactiva efecto; tests de logica pasan; rendering se mantiene estable.

4. Fase 4 - Animacion global de victoria:
- `UI-13.1` `src/SudokuArena.Desktop/Animations/CompletionAnimationPlanner.cs`: planificador de onda global sobre 81 celdas (anillos desde origen).
- `UI-13.2` `src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs`: metodo `StartVictoryAnimation(...)` con timing propio.
- `UI-13.3` `src/SudokuArena.Desktop/MainWindow.xaml.cs`: disparar animacion global al ganar y mostrar dialogo de victoria tras una espera corta.
- `UI-13.4` `test/SudokuArena.Desktop.Tests/CompletionAnimationPlannerTests.cs`: validar mapeo de distancias para onda global (centro/esquina).
- Criterio de aceptacion: al completar puzzle, se observa animacion en todo el tablero y luego aparece el dialogo.

### Feature: Inicio de Partida + Selector de Dificultad (Desktop)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| GS-01 | Definir contrato de dificultad para runtime (`Beginner/Easy/Medium/Hard/Expert`) | Alta | Completada | Implementado: `DifficultyTier` en Application + exposicion en `MainViewModel` (`SelectedDifficultyTier`, `DifficultyTierOptions`). |
| GS-02 | Agregar selector de dificultad en UI de inicio/nuevo juego | Alta | Completada | Implementado: `ComboBox` de dificultad en `MainWindow.xaml` enlazado a `SelectedDifficultyTier`. |
| GS-03 | Conectar `Nuevo Puzzle` para usar dificultad seleccionada | Alta | Completada | Implementado: `NewPuzzleCommand` consulta `IPuzzleProvider.GetNext(tier)` y notifica si no hay stock del nivel. |
| GS-04 | Persistir ultima dificultad elegida en settings locales | Media | Completada | Implementado: `IThemePreferenceStore` + `JsonThemePreferenceStore` ahora guardan/cargan `DifficultyTier` en `desktop-settings.json`; `MainViewModel` restaura al iniciar y persiste al cambiar. |
| GS-05 | Pruebas unitarias del flujo de seleccion y comando | Alta | Completada | Implementado: pruebas de selector/comando (`MainViewModelDifficultyTierTests`, `MainViewModelNewPuzzleTests`) y preferencia persistida (`MainViewModelDifficultyPreferenceTests`). |

Orden recomendado de ejecucion del feature:
1. `GS-01`
2. `GS-02`
3. `GS-03`
4. `GS-04`
5. `GS-05`

### Feature: Catalogo Runtime de Puzzles (Provider + JSON)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| PC-01 | Definir contrato `PuzzleDefinition` (id, grid, solution, difficulty, metadata) | Alta | Completada | Implementado: record tipado `PuzzleDefinition` en `src/SudokuArena.Application/Puzzles`. |
| PC-02 | Definir interfaz `IPuzzleProvider` para obtener puzzle por dificultad | Alta | Completada | Implementado: contrato `IPuzzleProvider.GetNext(DifficultyTier)` desacoplado de UI/fuente. |
| PC-03 | Implementar `JsonPuzzleProvider` (dataset local) | Alta | Completada | Implementado: provider en Infrastructure con validaciones de `schema_version`, ids, puzzle/solution, `solver_details` y `time_map`. |
| PC-04 | Politica de no repeticion y fallback por dificultad | Media | Completada | Implementado en `JsonPuzzleProvider`: resolucion por cercania de tier (fallback cuando el solicitado esta vacio) y seleccion evitando repeticion inmediata cuando hay mas de un puzzle en el tier efectivo. |
| PC-05 | Integrar provider con `MainViewModel`/`NewPuzzleCommand` | Alta | Completada | Implementado: DI en `App.xaml.cs` + dataset local `Data/puzzles.runtime.v1.json` consumido en runtime. |
| PC-06 | Pruebas unitarias del provider y de integracion VM | Alta | Completada | Implementado: `JsonPuzzleProviderTests` + pruebas de integracion de `MainViewModel` con provider por tier (solicitud por dificultad, carga y mensaje de fallback). |

Orden recomendado de ejecucion del feature:
1. `PC-01`
2. `PC-02`
3. `PC-03`
4. `PC-04`
5. `PC-05`
6. `PC-06`

### Feature: Integracion con `tools/puzzle_dataset` (Pipeline de datos)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| PD-01 | Definir esquema JSON final de consumo Desktop/Server (`schema_version`) | Alta | Completada | Implementado: `PuzzleDatasetDocument` (question_bank/solver_details/time_map) + doc `docs/SudokuArena/PuzzleDataset-Schema-v1.md`. |
| PD-02 | Agregar exportador de lote MVP para runtime (`puzzles.runtime.v1.json`) | Alta | Completada | Implementado: `tools/puzzle_dataset/generate_runtime_dataset.ps1` genera lote balanceado por tiers y sincroniza `PuzzleSeed/Data`. |
| PD-03 | Agregar validador de dataset en CI/local | Media | Completada | Implementado: `tools/puzzle_dataset/validate_runtime_dataset.ps1` valida schema, IDs, puzzle/solution, consistencia de givens, `solver_details`, `time_map` y solucion por `board_kind` (9x9/6x6/16x16). |
| PD-04 | Versionado de pesos/umbrales y trazabilidad de lote | Media | Propuesta | Salida minima: metadatos `weights_version`, `thresholds_version`, fecha y fuente del lote. |
| PD-05 | Empalme DS->PC (provider consume salida del tooling sin cambios manuales) | Alta | Completada | Implementado: `JsonPuzzleProvider` consume directo el schema v1; Desktop carga `PuzzleSeed/puzzles.runtime.v1.json` generado por tooling. |

Orden recomendado de ejecucion del feature:
1. `PD-01`
2. `PD-02`
3. `PD-05`
4. `PD-03`
5. `PD-04`

### Feature: Modos de Tablero Unificados (9x9 / 6x6 / 16x16)

| ID | Tarea | Prioridad | Estado | Nota |
|---|---|---|---|---|
| BM-01 | Definir contrato de `board_kind` y `mode` en Application | Alta | Completada | Implementado: `PuzzleBoardKind`, `PuzzleMode`, `PuzzleModeResolver`. |
| BM-02 | Extender schema/runtime para incluir `board_kind` y `mode` | Alta | Completada | Implementado en `PuzzleDatasetDocument`, generador runtime y schema doc v1. |
| BM-03 | Validar dataset por tipo de tablero (simbolos/longitud/reglas) | Alta | Completada | Implementado: validador soporta `Classic9x9`, `SixBySix`, `SixteenBySixteen`. |
| BM-04 | Ajustar provider Desktop para ignorar temporalmente tableros no soportados | Alta | Completada | Implementado: `JsonPuzzleProvider` carga solo `Classic9x9` y deja trazada compatibilidad futura. |
| BM-05 | Exponer selector de tamaño de tablero en UI (`9x9/6x6/16x16`) | Alta | Planificada | Salida minima: selector visible y persistencia local del board mode seleccionado. |
| BM-06 | Generalizar `SudokuBoard` para reglas dinamicas por tamaño | Alta | Planificada | Salida minima: validar jugadas y conflictos para 6x6 (2x3), 9x9 (3x3), 16x16 (4x4). |
| BM-07 | Implementar control grafico y UX de juego para 6x6 | Alta | Planificada | Salida minima: render, input, resaltado, borrado, victoria/derrota y pruebas basicas en 6x6. |
| BM-08 | Implementar control grafico y UX de juego para 16x16 | Media | Propuesta | Salida minima: render virtualizado/escala, input hexadecimal (1-9/A-G), reglas y pruebas. |
| BM-09 | Integrar seeds 6x6/16x16 al runtime oficial | Alta | Planificada | Salida minima: incluir bancos en `puzzles.runtime.v1.json` sin romper 9x9 ni pipeline de build/publish. |
| BM-10 | Estrategia de no repeticion global entre cliente y servidor por `board_kind+mode` | Media | Propuesta | Salida minima: contrato de asignacion determinista para evitar repeticiones entre seed local y partidas online. |

Orden recomendado de ejecucion del feature:
1. `BM-05`
2. `BM-06`
3. `BM-07`
4. `BM-09`
5. `BM-08`
6. `BM-10`

Orden transversal recomendado (para no pisar features):
1. `GS-01`
2. `PC-01`
3. `PC-02`
4. `PC-03`
5. `GS-02`
6. `GS-03`
7. `PC-04`
8. `PC-05`
9. `GS-04`
10. `GS-05`
11. `PC-06`
12. `PD-01`
13. `PD-02`
14. `PD-05`
15. `PD-03`
16. `PD-04`

Agrupacion por bloques de entrega (MVP a incremental):
1. Bloque A - Contratos y modelos base: `GS-01`, `PC-01`, `PC-02`, `PD-01`.
2. Bloque B - Flujo funcional en Desktop: `PC-03`, `GS-02`, `GS-03`, `PC-05`.
3. Bloque C - Calidad de seleccion y continuidad: `PC-04`, `GS-04`, `GS-05`, `PC-06`.
4. Bloque D - Pipeline de datos estable: `PD-02`, `PD-05`, `PD-03`, `PD-04`.

Subtareas tecnicas iniciales (AC/GS/PC/PD):

- `AC-01.1` `docs/SudokuArena/Registro-Avance-Ideas.md` y `docs/SudokuArena/Analisis-Referencia-EasySudoku.md`: documentar contrato funcional final de autocompletado para SudokuArena.
- `AC-02.1` `src/SudokuArena.Application` (nuevo servicio de reglas): evaluador de oportunidad de autocompletado desacoplado de UI.
- `AC-02.2` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: consumir evaluador y exponer estado/accion disponible en VM.
- `AC-03.1` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: comando de autocompletado que aplica jugada asistida en celda editable.
- `AC-03.2` `src/SudokuArena.Desktop/Controls/SudokuBoardControl.cs` (si aplica): refresco visual post-autocompletado sin romper resaltado/animaciones.
- `AC-04.1` `src/SudokuArena.Desktop/Settings/DesktopSettings.cs` y store asociado: persistir `AutoComplete` y settings relacionados del flujo asistido.
- `AC-04.2` `src/SudokuArena.Desktop/MainWindow.xaml`: confirmar enlace de toggles al VM y coherencia de estado al iniciar app.
- `AC-06.1` `test/SudokuArena.Desktop.Tests/*`: pruebas para toggles, evaluador y comando de autocompletado en escenarios positivos/negativos.

- `GS-01.1` `src/SudokuArena.Domain` o `src/SudokuArena.Application`: crear enum/valor `DifficultyTier` canonico para toda la solucion.
- `GS-01.2` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: exponer `SelectedDifficultyTier` y valor por defecto.
- `GS-02.1` `src/SudokuArena.Desktop/MainWindow.xaml`: agregar selector de dificultad conectado al VM.
- `GS-03.1` `src/SudokuArena.Desktop/ViewModels/MainViewModel.cs`: hacer que `NewPuzzleCommand` solicite puzzle por tier actual.
- `GS-03.2` `src/SudokuArena.Desktop/MainWindow.xaml.cs` (si aplica): mostrar feedback controlado cuando no haya stock para el tier.
- `GS-04.1` `src/SudokuArena.Desktop/Settings/DesktopSettings.cs`: persistir `SelectedDifficultyTier`.
- `GS-04.2` `src/SudokuArena.Desktop/Settings/DesktopSettingsStore.cs`: cargar/guardar el tier junto al resto de preferencias.

- `PC-01.1` `src/SudokuArena.Application/Puzzles`: definir `PuzzleDefinition` (id, puzzle, solucion, dificultad, metadata minima).
- `PC-02.1` `src/SudokuArena.Application/Puzzles`: definir `IPuzzleProvider` con metodo `GetNext(DifficultyTier tier)`.
- `PC-03.1` `src/SudokuArena.Infrastructure/Puzzles/JsonPuzzleProvider.cs`: implementar carga de dataset local y validacion basica.
- `PC-03.2` `src/SudokuArena.Infrastructure/Puzzles/PuzzleDatasetModels.cs`: DTOs para deserializacion versionada (`schema_version`).
- `PC-04.1` `src/SudokuArena.Infrastructure/Puzzles/JsonPuzzleProvider.cs`: politica anti-repeticion (ultima partida por tier).
- `PC-05.1` `src/SudokuArena.Desktop/CompositionRoot` o bootstrap actual: registrar `IPuzzleProvider` real para Desktop.

- `PD-01.1` `tools/puzzle_dataset`: definir schema runtime unico (`puzzle_dataset.v1.json`) documentado.
- `PD-02.1` `tools/puzzle_dataset`: exportar lote inicial con distribucion minima por tiers.
- `PD-05.1` `src/SudokuArena.Infrastructure/Puzzles/JsonPuzzleProvider.cs`: consumir schema sin transformaciones manuales.
- `PD-03.1` `tools/puzzle_dataset` + `test/*`: comando de validacion automatica del dataset.
- `PD-04.1` `tools/puzzle_dataset`: incluir metadatos de calibracion (`weights_version`, `thresholds_version`, `generated_at_utc`).

Criterios de aceptacion integrados (flujo Nuevo Juego):
1. Con dificultad seleccionada en UI, `Nuevo Puzzle` carga un puzzle de ese tier sin reiniciar app.
2. Si no hay puzzles en el tier, la app aplica fallback definido y lo comunica sin bloqueo.
3. Al reiniciar Desktop, se restaura la ultima dificultad seleccionada.
4. El provider no repite inmediatamente el mismo puzzle en llamadas consecutivas del mismo tier.
5. Tests de VM/provider cubren tier selection, fallback y no repeticion basica.

## Proxima Ola Recomendada
1. Conectar desktop realmente a server (SignalR + API) y definir flujo LAN de punta a punta.
2. Implementar perfil de jugador persistido + autenticacion real.
3. Implementar sistema ELO y base de estadisticas.
4. Implementar torneos (MVP) con modelo de dominio y endpoints.

## Regla de Actualizacion
- Cada feature terminada mueve items de `Parcial` o `Pendiente` a `Hecho`.
- Cada decision importante se agrega con fecha en commit y, si aplica, en `Architecture.md`.
- Cada idea nueva se registra en la tabla de backlog con prioridad inicial.
- Toda sigla nueva usada en este documento o docs relacionadas debe agregarse en `docs/SudokuArena/Leyenda-Siglas.md`; si se elimina su uso, se elimina tambien de la leyenda.
