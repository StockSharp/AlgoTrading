# Estrategia SuperTrend de Reversión de Pivot Mejorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina la dirección del SuperTrend con rupturas de máximos/mínimos de pivot. Se coloca un stop de compra por encima de un máximo de pivot reciente cuando el SuperTrend es bajista. Se coloca un stop de venta por debajo de un mínimo de pivot cuando el SuperTrend es alcista. Las posiciones están protegidas con un stop-loss porcentual desde el pivot.

## Detalles

- **Criterios de entrada**:
  - Largo: Formado un máximo de pivot, SuperTrend bajista → stop de compra por encima del pivot.
  - Corto: Formado un mínimo de pivot, SuperTrend alcista → stop de venta por debajo del pivot.
- **Dirección**: Configurable.
- **Criterios de salida**: Stop-loss porcentual o dirección opuesta para el modo unilateral.
- **Indicadores**: SuperTrend, máximos/mínimos de pivot.
- **Valores predeterminados**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 minute
