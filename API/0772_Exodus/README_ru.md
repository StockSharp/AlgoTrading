# Стратегия Exodus
[English](README.md) | [中文](README_cn.md)

Упрощённая адаптация скрипта TradingView **EXODUS** для StockSharp. Стратегия использует объёмно-взвешенный импульс VWMO и индикатор ADX для поиска сильных направленных движений.

## Детали

- **Условия входа**
  - Лонг: `VWMO > VwmoThreshold` и `ADX > AdxThreshold`.
  - Шорт: `VWMO < -VwmoThreshold` и `ADX > AdxThreshold`.
- **Условия выхода**
  - Импульс пересекает ноль или появляется обратный сигнал.
- **Индикаторы**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Параметры**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
