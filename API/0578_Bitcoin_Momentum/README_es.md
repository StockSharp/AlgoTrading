# Estrategia de Momentum de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de momentum para Bitcoin que opera solo cuando el precio está por encima de una EMA de marco temporal superior y evita condiciones de precaución. Un stop trailing basado en ATR protege las ganancias.

## Detalles

- **Criterios de entrada**: Precio por encima de la EMA semanal y sin condición de precaución.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Precio por debajo del stop trailing o la EMA semanal.
- **Stops**: Stop trailing basado en ATR.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: EMA, ATR, Highest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
