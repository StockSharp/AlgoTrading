# Estrategia de Rompimiento de Liquidez de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en posiciones largas cuando la liquidez y la volatilidad son altas y la tendencia a corto plazo es alcista. La alta liquidez se define como volumen por encima de su media móvil multiplicado por un umbral. La volatilidad se confirma cuando el ATR supera su media móvil.

## Detalles

- **Criterios de entrada**:
  - `Volumen > SMA(volumen) * LiquidityThreshold`
  - `Cambio de precio (%) > PriceChangeThreshold`
  - `SMA rápida > SMA lenta`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: SMA rápida cruzando por debajo de la SMA lenta o RSI > 70.
- **Stops**: Porcentajes opcionales de stop-loss y toma de ganancias.
- **Valores predeterminados**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: SMA, RSI, ATR
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: 1h
