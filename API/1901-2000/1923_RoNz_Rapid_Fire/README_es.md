# Estrategia de Fuego Rápido RoNz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una media móvil con el indicador Parabolic SAR para detectar cambios rápidos de tendencia. Se abre una posición larga cuando el precio de cierre supera la media móvil mientras el Parabolic SAR gira por debajo del precio. Se abre una posición corta en las condiciones opuestas. Las posiciones pueden promediarse opcionalmente cuando la tendencia continúa.

## Cómo Funciona
- **Entrada largo**: Precio de cierre > SMA y Parabolic SAR cambia a por debajo del precio.
- **Entrada corto**: Precio de cierre < SMA y Parabolic SAR cambia a por encima del precio.
- **Cierre**: Por stop loss/take profit o por señal opuesta según el modo seleccionado.
- **Promediado**: Añade nuevas posiciones cuando la tendencia persiste.
- **Trailing Stop**: Ajusta el precio de stop a medida que la operación avanza en beneficio.

## Parámetros
- `Volume` – volumen de la operación.
- `StopLoss` – stop loss en ticks.
- `TakeProfit` – take profit en ticks.
- `TrailingStop` – trailing stop en ticks.
- `Averaging` – activar promediado de posiciones.
- `MaPeriod` – período de la media móvil.
- `PsarStep` – paso del Parabolic SAR.
- `PsarMax` – valor máximo del Parabolic SAR.
- `CloseType` – `SlClose` usa solo stops, `TrendClose` cierra en tendencia opuesta.
- `CandleType` – serie de velas para los cálculos.

## Notas
- Funciona con cualquier instrumento compatible con StockSharp.
- Requiere velas históricas para el `CandleType` seleccionado.
