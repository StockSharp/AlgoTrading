# Estrategia Donky MA TP SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces de medias móviles con dos objetivos de take-profit y un stop-loss. Entra largo cuando la SMA rápida cruza por encima de la SMA lenta y corto cuando cruza por debajo. La mitad de la posición se cierra en el primer objetivo y el resto en el segundo objetivo o el stop-loss.

## Detalles

- **Criterios de entrada**:
  - **Largo**: SMA rápida cruza por encima de la SMA lenta.
  - **Corto**: SMA rápida cruza por debajo de la SMA lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Dos niveles fijos de take-profit o un stop-loss fijo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
