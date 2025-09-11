# Exodus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a simplified port of the TradingView **EXODUS** script. It uses a volume weighted momentum oscillator (VWMO) together with the Average Directional Index to detect strong directional moves.

## Details

- **Entry Criteria**
  - Long: `VWMO > VwmoThreshold` and `ADX > AdxThreshold`.
  - Short: `VWMO < -VwmoThreshold` and `ADX > AdxThreshold`.
- **Exit Criteria**
  - Momentum crosses zero or an opposite signal appears.
- **Indicators**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Parameters**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
