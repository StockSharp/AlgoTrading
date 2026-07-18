# Estrategia de Backtesting de Riesgo/Recompensa con SL Fijo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra largo cuando el precio de cierre coincide con un valor definido por el usuario. El stop loss se establece por ATR o mínimo de pivote y el take profit usa una relación riesgo/recompensa o porcentaje fijo. Opcionalmente mueve el stop a breakeven después de alcanzar un objetivo.

## Detalles

- **Criterios de entrada**: precio de cierre igual a `DealStartValue`
- **Largo/Corto**: Largo
- **Criterios de salida**: take profit o stop loss (breakeven opcional)
- **Stops**: ATR o mínimo de pivote con breakeven
- **Valores predeterminados**:
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: ATR, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
