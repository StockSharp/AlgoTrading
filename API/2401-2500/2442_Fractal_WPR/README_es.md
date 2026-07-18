# Estrategia Fractal WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador Williams %R para generar señales de trading basadas en cruces de los niveles de sobrecompra y sobreventa. Está adaptada de un asesor experto MQL5 y demuestra un sistema simple de reversión de momentum.

## Cómo funciona

1. Se calcula un indicador Williams %R con período configurable en el marco temporal seleccionado.
2. Dos niveles horizontales definen las zonas extremas:
   - `HighLevel` marca la zona de sobrecompra (por defecto −30).
   - `LowLevel` marca la zona de sobreventa (por defecto −70).
3. Cuando `Trend` está configurado como `Direct`:
   - Cruzar hacia abajo `LowLevel` abre una posición larga y cierra cualquier posición corta.
   - Cruzar hacia arriba `HighLevel` abre una posición corta y cierra cualquier posición larga.
4. Cuando `Trend` está configurado como `Against`, las reacciones a los cruces se invierten.
5. Los parámetros opcionales permiten habilitar o deshabilitar por separado la apertura y el cierre de posiciones largas o cortas.
6. Las distancias de stop‑loss y take‑profit en ticks se aplican mediante la API de protección de alto nivel.

Solo se procesan las velas completadas para evitar reaccionar al ruido intrabarra.

## Parámetros

- `WprPeriod` – período de cálculo de Williams %R.
- `HighLevel` – umbral para la zona de sobrecompra.
- `LowLevel` – umbral para la zona de sobreventa.
- `Trend` – modo de trading (`Direct` o `Against`).
- `BuyPositionOpen` – permitir abrir posiciones largas.
- `SellPositionOpen` – permitir abrir posiciones cortas.
- `BuyPositionClose` – permitir cerrar posiciones largas.
- `SellPositionClose` – permitir cerrar posiciones cortas.
- `StopLossTicks` – distancia del stop‑loss en ticks.
- `TakeProfitTicks` – distancia del take‑profit en ticks.
- `CandleType` – marco temporal de velas utilizado para el análisis.

## Indicadores

- Williams %R
