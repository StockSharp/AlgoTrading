# Estrategia HMA Crossover RSI Stochastic Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que usa el cruce de HMA rápida y lenta con filtros RSI y Stochastic suavizado. Abre largo cuando la HMA rápida cruza por encima de la lenta con RSI y Stochastic por debajo de los umbrales, y abre corto en la condición opuesta. Un stop de seguimiento gestiona las salidas.

## Detalles

- **Criterios de entrada**: Cruce de HMA rápida sobre lenta con RSI y Stochastic por debajo de los umbrales.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop de seguimiento o señal opuesta.
- **Stops**: Porcentaje de seguimiento.
- **Valores predeterminados**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: HMA, RSI, Stochastic
  - Stops: Trailing
  - Complejidad: Básico
  - Marco temporal: 1h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
