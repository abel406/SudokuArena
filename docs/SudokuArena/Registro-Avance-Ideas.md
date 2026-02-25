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
  - escala de dificultad propuesta basada en solver-techniques/SE rate (con umbrales iniciales),
  - pendiente aterrizar propuesta final para backend LAN/offline+sync y validar umbrales con dataset completo.
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

| Idea | Prioridad | Estado | Nota |
|---|---|---|---|
|  | Alta / Media / Baja | Propuesta / En analisis / Planificada / En curso / Hecha |  |

## Proxima Ola Recomendada
1. Conectar desktop realmente a server (SignalR + API) y definir flujo LAN de punta a punta.
2. Implementar perfil de jugador persistido + autenticacion real.
3. Implementar sistema ELO y base de estadisticas.
4. Implementar torneos (MVP) con modelo de dominio y endpoints.

## Regla de Actualizacion
- Cada feature terminada mueve items de `Parcial` o `Pendiente` a `Hecho`.
- Cada decision importante se agrega con fecha en commit y, si aplica, en `Architecture.md`.
- Cada idea nueva se registra en la tabla de backlog con prioridad inicial.
