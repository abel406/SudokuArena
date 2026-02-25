# Registro de Avance e Ideas (SudokuArena)

Este documento sirve como registro vivo de:
- lo implementado,
- lo parcialmente implementado,
- lo pendiente,
- y nuevas ideas para priorizar.

Actualizado: 2026-02-25

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
- Autocompletado: existe toggle en UI, falta logica funcional.

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

## Proxima Ola Recomendada
1. Conectar desktop realmente a server (SignalR + API) y definir flujo LAN de punta a punta.
2. Implementar perfil de jugador persistido + autenticacion real.
3. Implementar sistema ELO y base de estadisticas.
4. Implementar torneos (MVP) con modelo de dominio y endpoints.

## Regla de Actualizacion
- Cada feature terminada mueve items de `Parcial` o `Pendiente` a `Hecho`.
- Cada decision importante se agrega con fecha en commit y, si aplica, en `Architecture.md`.
- Cada idea nueva se registra en la tabla de backlog con prioridad inicial.
