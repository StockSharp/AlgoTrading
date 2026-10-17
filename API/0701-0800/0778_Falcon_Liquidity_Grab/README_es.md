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

**Regla precisa y valores predeterminados:** `SwingPeriod=20`, `TrendLength=50`, `StopLossPoints=50`, `TakeProfitMultiplier=2`, `UseSession=true`, `SessionStart=07:00`, `SessionEnd=21:00` UTC, `CandleType=15 minutos`. El mínimo/máximo de la vela cerrada se compara con los extremos de las N velas **anteriores** (sin incluir la actual). Largo si Low está estrictamente bajo el mínimo previo y Close sobre la SMA; corto si High supera el máximo previo y Close está bajo SMA. La SMA sí incluye la vela de señal. Solo se entra sin posición dentro de la ventana UTC (inicio incluido, fin excluido; admite cruce de medianoche). La protección nativa mediante órdenes de mercado usa bid/ask Level1 entre velas: stop `StopLossPoints × Security.PriceStep`, objetivo `TakeProfitMultiplier × distancia del stop`. No hay salida por señal contraria al cierre.
