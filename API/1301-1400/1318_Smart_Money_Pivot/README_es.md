# Estrategia de Pivote Smart Money
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de máximos y mínimos de pivote. Una posición larga se abre cuando el precio supera el último pivote alto, mientras que una posición corta se abre cuando el precio cae por debajo del último pivote bajo. Cada operación utiliza sus propios porcentajes de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: Ruptura por encima del pivote alto o por debajo del pivote bajo.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
