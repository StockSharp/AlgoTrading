# Hybrid RSI Breakout Dashboard
[English](README.md) | [Русский](README_ru.md)

该策略结合 RSI 均值回归和基于 ADX 与 200 EMA 的突破交易。

当市场震荡且 RSI 低于 `RsiBuy` 且价格高于 EMA 时做多；当 RSI 高于 `RsiSell` 且价格低于 EMA 时做空。处于趋势时，突破最近 `BreakoutLength` 根收盘价的高/低点开仓，并使用 ATR 跟踪止损。

包含起始日期过滤以及用于显示最后一次交易类型和方向的变量。

## 细节

- **入场条件**：在震荡阶段使用 RSI + EMA 过滤，或在 ADX > `AdxThreshold` 时突破过去 `BreakoutLength` 根收盘价。
- **多空方向**：双向。
- **出场条件**：RSI 交易在 `RsiExit` 处平仓；突破交易使用 ATR 跟踪止损。
- **止损**：ATR 跟踪止损（仅突破交易）。
- **默认值**：
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：趋势、均值回归
  - 方向：双向
  - 指标：ADX、EMA、RSI、ATR、Highest/Lowest
  - 止损：跟踪
  - 复杂度：中
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
