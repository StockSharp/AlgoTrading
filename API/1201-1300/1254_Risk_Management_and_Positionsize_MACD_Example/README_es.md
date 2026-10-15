# Gestión de Riesgo y Tamaño de Posición - Ejemplo con MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Gestión de Riesgo y Tamaño de Posición - Ejemplo con MACD** demuestra el dimensionamiento dinámico de posiciones basado en la equidad actual. Se basa en cruces de MACD de un marco temporal superior combinados con un filtro de tendencia de media móvil.

## Detalles
- **Criterios de entrada**: La línea MACD cruza por encima/debajo de la línea de señal con confirmación de tendencia.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce MACD opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `InitialBalance = 10000m`
  - `LeverageEquity = true`
  - `MarginFactor = -0.5m`
  - `Quantity = 3.5m`
  - `MacdMaType = MovingAverageTypeEnum.EMA`
  - `FastMaLength = 11`
  - `SlowMaLength = 26`
  - `SignalMaLength = 9`
  - `MacdTimeFrame = TimeSpan.FromMinutes(30)`
  - `TrendMaType = MovingAverageTypeEnum.EMA`
  - `TrendMaLength = 55`
  - `TrendTimeFrame = TimeSpan.FromDays(1)`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, Moving Average
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
