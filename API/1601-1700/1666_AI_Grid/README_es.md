# Estrategia de Cuadrícula AI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cuadrícula AI coloca órdenes de compra y venta en capas alrededor del precio actual. La estrategia admite enfoques de ruptura (stop) y contratendencia (límite). Después de que se ejecuta una orden, se coloca automáticamente una orden de take-profit.

## Detalles

- **Criterios de entrada**: El precio alcanza uno de los niveles de la cuadrícula.
- **Largo/Corto**: Controlado mediante `AllowLong` y `AllowShort`.
- **Criterios de salida**: Take-profit después de una distancia fija `TakeProfit`.
- **Stops**: Sin stop-loss.
- **Valores predeterminados**:
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Grid
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Solo take-profit
  - Complejidad: Intermedio
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
