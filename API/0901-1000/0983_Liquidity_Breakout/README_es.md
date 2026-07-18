# Estrategia de Rompimiento de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos de un rango de precios reciente definido por máximos y mínimos pivote. Se abre una posición cuando el precio cierra más allá de los extremos del rango anterior. El stop loss opcional puede usar una línea SuperTrend o un porcentaje fijo.

## Detalles

- **Criterios de entrada**:
  - `Cierre > máximo del rango anterior` → largo
  - `Cierre < mínimo del rango anterior` → corto
- **Largo/Corto**: Configurable (Largo, Corto, Ambos).
- **Criterios de salida**: Rompimiento opuesto o stop loss.
- **Stops**: SuperTrend o porcentaje fijo.
- **Valores predeterminados**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, SuperTrend
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: 1h
