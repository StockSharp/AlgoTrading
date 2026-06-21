# Estrategia de Canal Mustang Algo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza un oscilador de sentimiento global basado en RSI suavizado con WMA para operar cruces de canal.

## Detalles

- **Criterios de entrada**: Cruces del oscilador RSI/WMA con los límites.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Señal opuesta o stop/take.
- **Stops**: Porcentuales, opcionales.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: RSI, WMA
  - Stops: Porcentaje
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
