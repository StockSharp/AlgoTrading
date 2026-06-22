# Estrategia del Panel de Operaciones en Autopiloto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce la lógica central del experto MQL4 original "trade panel with autopilot". Agrega la dirección del precio en múltiples marcos temporales y abre o cierra una única posición según el sentimiento dominante del mercado.

La estrategia monitorea las dos últimas velas en ocho marcos temporales diferentes (M1, M5, M15, M30, H1, H4, D1, W1). Para cada marco temporal compara varios componentes de precio entre las dos velas más recientes:

- Open
- High
- Low
- (High + Low) / 2
- Close
- (High + Low + Close) / 3
- (High + Low + Close + Close) / 4

Cada comparación contribuye a una puntuación de **compra** o **venta**. Las puntuaciones de todos los marcos temporales se suman y se convierten en porcentajes. Cuando el porcentaje de compra o venta cruza un umbral configurado, la estrategia entra en una posición. La posición existente se cierra si el porcentaje opuesto cae por debajo del umbral de cierre.

## Parámetros

- `Autopilot` — activa o desactiva el trading automático.
- `OpenThreshold` — nivel de porcentaje requerido para abrir una nueva posición. Por defecto: 85.
- `CloseThreshold` — nivel de porcentaje para cerrar una posición existente. Por defecto: 55.
- `LotFixed` — volumen fijo de la orden cuando `UseFixedLot` está habilitado.
- `LotPercent` — volumen como porcentaje del valor de la cartera cuando `UseFixedLot` está deshabilitado.
- `UseFixedLot` — alterna entre volumen fijo y porcentual.
- `UseStopLoss` — inicia la protección de la posición cuando está habilitado.

## Lógica de trading

1. Suscribirse a las velas en todos los marcos temporales configurados.
2. Calcular las puntuaciones de compra/venta para cada nueva vela completada.
3. Sumar las puntuaciones por marcos temporales y calcular los porcentajes de compra/venta.
4. Si `Autopilot` está deshabilitado, la estrategia solo realiza seguimiento de las puntuaciones.
5. Si no hay posición abierta y el porcentaje de compra supera `OpenThreshold`, entrar en una posición larga. Si el porcentaje de venta supera el umbral, entrar en una posición corta.
6. Si existe una posición larga y el porcentaje de compra cae por debajo de `CloseThreshold`, salir de la posición. La misma lógica aplica para posiciones cortas usando el porcentaje de venta.

## Notas

- La estrategia mantiene como máximo una posición abierta a la vez.
- La gestión opcional de stop-loss se activa mediante `StartProtection()` cuando `UseStopLoss` es verdadero.
