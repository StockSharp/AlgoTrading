# Estrategia de Umbral CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compra cuando el CCI cae por debajo de un umbral y sale cuando el precio de cierre supera el cierre anterior.
Stop loss y take profit opcionales en puntos absolutos.

## Detalles

- **Criterios de entrada**:
  - Largo: `CCI < BuyThreshold`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - `ClosePrice > previous ClosePrice`
- **Stops**: Opcional mediante `UseStopLoss` y `UseTakeProfit`
- **Valores predeterminados**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: CCI
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
