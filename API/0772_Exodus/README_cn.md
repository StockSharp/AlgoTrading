# Exodus 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 TradingView **EXODUS** 脚本的简化移植版。它结合了成交量加权动量指标 VWMO 与 ADX 趋势强度指标，用于捕捉方向性突破。

## 细节

- **入场条件**
  - 多头：`VWMO > VwmoThreshold` 且 `ADX > AdxThreshold`。
  - 空头：`VWMO < -VwmoThreshold` 且 `ADX > AdxThreshold`。
- **出场条件**
  - 动量穿越零线或出现反向信号。
- **指标**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **参数**
  - `VwmoMomentum`、`VwmoVolume`、`VwmoSmooth`、`VwmoThreshold`
  - `AtrLength`、`AtrMultiplier`、`TpMultiplier`
  - `AdxLength`、`AdxThreshold`
  - `Volume`
  - `CandleType`
