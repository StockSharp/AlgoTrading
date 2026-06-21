# RSI Value
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera basándose en el Índice de Fuerza Relativa (RSI) al cruzar un valor central.

La idea es observar cuando el RSI cruza por encima o por debajo de un nivel configurable (por defecto 50). Cuando el indicador se mueve de abajo hacia arriba de este nivel se abre una posición larga. Cuando cruza de vuelta hacia abajo se abre una posición corta. Las posiciones existentes se cierran en el cruce opuesto. Stop-loss opcional, take-profit y trailing stop protegen la operación.

## Detalles

- **Criterios de entrada**: Comprar cuando RSI cruza por encima del nivel. Vender cuando RSI cruza por debajo.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto o trailing stop.
- **Stops**: Stop-loss fijo opcional, take-profit y trailing stop.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `RsiLevel` = 50
  - `StopLoss` = 100
  - `TakeProfit` = 200
  - `TrailingStop` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
