# Estrategia de Ruptura de Acción de Precio Pura con RR 1:5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura de Acción de Precio Pura con RR 1:5 utiliza el cruce de dos EMAs confirmado por RSI y volumen. El stop loss se basa en ATR y el take profit es cinco veces el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida cruza por encima de EMA lenta, RSI > 50, volumen por encima de la SMA de 20 períodos.
  - **Corto**: EMA rápida cruza por debajo de EMA lenta, RSI < 50, volumen por encima de la SMA de 20 períodos.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop loss basado en ATR y take profit con riesgo-recompensa 1:5.
- **Stops**: Stop loss = 1.5 × ATR, take profit = 5 × riesgo.
- **Valores predeterminados**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ATR, Volume SMA
  - Stops: Stop loss ATR, take profit 1:5
  - Complejidad: Bajo
  - Marco temporal: 5m o 15m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
