# Estrategia CP Strat ORB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rompimientos del rango de apertura de Nueva York (9:30-9:45) con un retesteo. Entra en largo después de que el precio rompe por encima del máximo del rango y cierra nuevamente por encima de él, y entra en corto después de que el precio rompe por debajo del mínimo del rango y cierra nuevamente por debajo. Las salidas utilizan niveles fijos de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: Rompimiento del rango de apertura de NY seguido de un retesteo y cierre más allá del límite del rango.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take-profit o stop-loss fijo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MinRangePoints` = 60m
  - `StopPoints` = 20m
  - `TakePoints` = 60m
  - `MaxTradesPerSession` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
