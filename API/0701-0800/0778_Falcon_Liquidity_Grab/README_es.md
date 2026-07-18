# Estrategia Falcon Liquidity Grab
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera capturas de liquidez durante las principales sesiones del mercado utilizando una media móvil simple para definir la tendencia. Entra cuando el precio traspasa niveles de swing recientes y revierte con la tendencia. Cada operación utiliza stop loss y take profit fijos medidos en ticks.

## Detalles

- **Condiciones de entrada**:
  - **Largo**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **Corto**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **Condiciones de salida**: stop loss y take profit fijos.
- **Tipo**: Reversión
- **Indicadores**: SMA, Highest, Lowest
- **Marco temporal**: 15 minutos (por defecto)
- **Stops**: `StopLossPoints` ticks, `TakeProfitMultiplier`× distancia del stop
