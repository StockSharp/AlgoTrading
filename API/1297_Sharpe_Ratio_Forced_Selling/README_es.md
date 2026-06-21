# Venta Forzada por Sharpe Ratio — Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Venta Forzada por Sharpe Ratio entra en largo cuando el Sharpe Ratio móvil cae por debajo de un umbral negativo y sale cuando sube por encima de un umbral positivo o el período de mantenimiento supera un límite. Los rendimientos pueden calcularse mediante cambios logarítmicos o simples y ajustarse por una tasa libre de riesgo.

## Detalles

- **Criterios de entrada**: Sharpe Ratio por debajo de `EntrySharpeThreshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Sharpe Ratio por encima de `ExitSharpeThreshold` o el período de mantenimiento supera `MaxHoldingDays`.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 8
  - `EntrySharpeThreshold` = -5
  - `ExitSharpeThreshold` = 13
  - `MaxHoldingDays` = 80
  - `UseLogReturns` = true
  - `RiskFreeRateAnnual` = 0
  - `PeriodsPerYear` = 252
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Sharpe Ratio
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
