# Estrategia Ilan Dynamic HT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de martingala basada en cuadrícula que abre posiciones basándose en señales RSI y amplía la posición utilizando un rango de precios dinámico. Cada operación adicional aumenta el volumen por un multiplicador y comparte el mismo take profit y stop loss.

## Detalles

- **Criterios de entrada**:
  - Largo: RSI por debajo de `RsiMinimum`
  - Corto: RSI por encima de `RsiMaximum`
- **Largo/Corto**: Largo y Corto
- **Criterios de salida**:
  - Se alcanza el take profit o stop loss común
- **Stops**:
  - `TakeProfit` en puntos
  - `StopLoss` en puntos
- **Valores predeterminados**:
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Cuadrícula / Martingala
  - Dirección: Largo y Corto
  - Indicadores: RSI, Highest, Lowest
  - Stops: Take Profit, Stop Loss
  - Complejidad: Avanzado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
