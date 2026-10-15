# Estrategia MA L World
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de medias móviles ponderadas con stop dinámico basado en EMA.

Abre una posición larga cuando la WMA rápida cruza por encima de la WMA lenta. Abre una posición corta cuando la WMA rápida cruza por debajo de la WMA lenta. Utiliza una EMA de 92 períodos como salida dinámica y niveles fijos de stop loss y take profit.

## Detalles

- **Criterios de entrada**:
  - Largo: `WMA Rápida` cruza por encima de `WMA Lenta`
  - Corto: `WMA Rápida` cruza por debajo de `WMA Lenta`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce opuesto o precio que cruza la EMA dinámica
- **Stops**: Stop loss y take profit mediante `StartProtection`
- **Valores predeterminados**:
  - `FastMaLength` = 12
  - `SlowMaLength` = 25
  - `TrailingMaPeriod` = 92
  - `StopLoss` = 95m
  - `TakeProfit` = 670m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: WMA, EMA
  - Stops: Stop loss, take profit, EMA dinámica
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
