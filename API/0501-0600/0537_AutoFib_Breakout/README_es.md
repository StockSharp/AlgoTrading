# Estrategia de Ruptura AutoFib
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traza una extensión de Fibonacci dinámica desde el máximo y mínimo recientes y abre una posición larga cuando el precio rompe por encima del nivel 1.618 durante una tendencia alcista definida por la EMA de 200 períodos. El riesgo se gestiona mediante un stop y objetivo basados en ATR.

## Detalles

- **Criterios de entrada**: Cierre por encima de la extensión 1.618 de Fibonacci y por encima de la EMA200.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop-loss basado en ATR o toma de ganancias de 3×ATR.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `FibLevel` = 1.618
  - `PivotPeriod` = 10
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: EMA, ATR, Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
