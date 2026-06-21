# Estrategia Diaria Supertrend EMA Crossover con Filtro RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera cruces de EMA solo cuando el Supertrend confirma la dirección y el RSI es favorable. Utiliza niveles de stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Fast EMA` cruza por encima de `Slow EMA`, Supertrend en tendencia alcista, `RSI < RsiOverbought`
  - Corto: `Fast EMA` cruza por debajo de `Slow EMA`, Supertrend en tendencia bajista, `RSI > RsiOversold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss o take profit basado en ATR
- **Stops**: Sí
- **Valores predeterminados**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, Supertrend, RSI, ATR
  - Stops: Múltiplos de ATR
  - Complejidad: Intermedio
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
