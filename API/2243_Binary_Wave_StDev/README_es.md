# Estrategia Binary Wave StdDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que suma señales de MA, MACD, CCI, Momentum, RSI y ADX usando pesos configurables.
Opera en la dirección de la puntuación acumulada cuando la volatilidad medida por la desviación estándar supera un umbral.
Stop loss y take profit opcionales en puntos.

## Detalles

- **Criterios de entrada**:
  - Largo: puntuación > 0 y StdDev >= EntryVolatility
  - Corto: puntuación < 0 y StdDev >= EntryVolatility
- **Criterios de salida**:
  - La volatilidad cae por debajo de ExitVolatility
- **Stops**: Opcional mediante `UseStopLoss` y `UseTakeProfit`
- **Valores predeterminados**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
