# 简单RSI股票策略 1D
[English](README.md) | [Русский](README_ru.md)

当RSI跌破超卖水平且价格高于200日SMA时，本系统做多。持仓使用ATR止损并设定三个获利目标。

## 详情

- **入场条件**: RSI低于`OversoldLevel`且收盘价在SMA之上。
- **多空方向**: 仅做多。
- **出场条件**: ATR止损或任一止盈水平被触发。
- **止损**: 是。
- **默认值**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - 类别: Oscillator
  - 方向: Long
  - 指标: RSI, SMA, ATR
  - 止损: 是
  - 复杂度: Basic
  - 时间框架: Daily
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: Medium
