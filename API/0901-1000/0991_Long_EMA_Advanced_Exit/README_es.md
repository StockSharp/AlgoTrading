# Long EMA con Salida Avanzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long EMA con Salida Avanzada es una estrategia solo de largos que entra cuando una media móvil corta cruza hacia arriba de una media, y el precio está por encima de una media móvil larga. Las salidas pueden activarse por cruce bajista del MACD, cierre del precio por debajo de la media móvil seleccionada, cruce bajista de la MA, stop trailing o un filtro de volatilidad basado en ATR.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La MA corta cruza hacia arriba la MA media y el precio está por encima de la MA larga.
- **Criterios de salida**: Cruce bajista del MACD, precio por debajo de la MA seleccionada, cruce bajista de la MA corta por debajo de la MA media, stop trailing opcional.
- **Stops**: Stop trailing opcional.
- **Valores predeterminados**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: MA, MACD, ATR
  - Complejidad: Medio
  - Nivel de riesgo: Medio
