# Estrategia del Máximo de Ayer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura en largo que coloca una orden de compra stop por encima del máximo del día anterior.
Filtro ROC opcional, stop trailing y cierre por EMA proporcionan control adicional de riesgo.

## Detalles

- **Criterios de entrada**: Cierre por debajo del máximo de ayer, luego buy stop en máximo + gap
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Stop-loss, take-profit, stop trailing opcional o cruce de EMA
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: Price, ROC, EMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
