# Playbook: Calibracion de Dificultad y Tiempo (DS-04 / DS-05)

Objetivo: mantener un proceso reproducible para ajustar pesos de tecnicas y umbrales de tiempo sin decisiones ad-hoc.

## 1) Objetivos de salida

- Distribucion objetivo de tiers (ejemplo inicial): `20/20/20/20/15/5` para `Beginner/Easy/Medium/Hard/Expert/Extreme`.
- Correlacion minima esperada entre dificultad y tiempo real/estimado: `Spearman >= 0.80`.
- Estabilidad inter-versiones: evitar saltos bruscos de pesos y umbrales.

## 2) Semilla inicial de pesos

- Definir un orden de tecnicas de menor a mayor complejidad.
- Asignar pesos iniciales propios con escala suave (ejemplo: `1.0` a `7.0`).
- No copiar tablas de terceros; solo usar conocimiento general de Sudoku.

## 3) Metricas por puzzle

Para cada puzzle calcular:

- `weighted_se = sum(count(technique) * weight(technique))`
- `max_rate = max(weight(technique) con count > 0)`
- `advanced_hits = sum(count) de tecnicas avanzadas (definidas por catalogo propio)`

## 4) Restricciones de optimizacion

- Monotonia: una tecnica mas dificil no puede tener menor peso que una tecnica mas simple.
- Suavidad: diferencia maxima entre tecnicas vecinas (ejemplo inicial `<= 1.2`).
- Penalizacion por drift: minimizar `||w_new - w_prev||` para mantener continuidad.

## 5) Iteracion de calibracion

- Ajustar pesos en pasos pequenos (`+-0.1` / `+-0.2`).
- Recalcular distribucion de tiers y correlacion.
- Conservar la configuracion que mejor cumpla objetivos + restricciones.

## 6) Umbrales de tiempo (`time_map`)

- Definir `time_map[qid] = [t1,t2,t3,t4]` por percentiles propios.
- Punto de partida recomendado: percentiles `35/55/75/90`.
- Ajustar luego con telemetria real de partidas.

## 7) Versionado obligatorio

Cada corrida de calibracion debe persistir:

- `weights_version`
- `thresholds_version`
- fecha/hora
- metodo (algoritmo/parametros)
- metricas de resultado (distribucion de tiers, correlacion, drift)

No se aceptan cambios manuales "a ojo" sin registro de experimento.

## 8) Entregables minimos DS-04 / DS-05

- Tabla `v1` de pesos calibrados.
- Tabla `v1` de umbrales por tier.
- Generacion de `time_map` versionada.
- Reporte comparativo contra version anterior.
