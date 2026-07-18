# Estrategia Tomas Ratio con Análisis Multi-Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia acumula ganancias y pérdidas ponderadas a través de múltiples marcos temporales para construir una señal Tomas Ratio. Las operaciones se abren cuando la fuerza de la señal supera un objetivo y se cierran cuando la debilidad domina.

## Detalles

- **Criterios de entrada**: la fuerza de la señal supera el objetivo y el precio está por encima de EMA(720)
- **Largo/Corto**: Solo largos
- **Criterios de salida**: los puntos de cierre superan los puntos de compra
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = velas de 1 hora
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Solo largos
  - Indicadores: Standard Deviation, SMA, EMA
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Múltiples
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
