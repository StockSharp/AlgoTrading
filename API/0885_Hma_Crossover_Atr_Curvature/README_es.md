# HMA Crossover ATR Curvature
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

HMA Crossover ATR Curvature es una estrategia de seguimiento de tendencia que combina un cruce de Hull Moving Average rápida y lenta con un filtro de curvatura. El tamaño de la posición se basa en el ATR y el porcentaje de riesgo, y las operaciones están protegidas por un stop de seguimiento basado en ATR.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La HMA rápida cruza por encima de la HMA lenta y la curvatura está por encima del umbral.
  - **Corto**: La HMA rápida cruza por debajo de la HMA lenta y la curvatura está por debajo del umbral negativo.
- **Criterios de salida**: Stop de seguimiento ATR.
- **Stops**: Stop de seguimiento ATR.
- **Valores predeterminados**:
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo & Corto
  - Indicadores: HMA, ATR
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
