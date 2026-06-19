# Megabar突破 (Range & Volume & RSI)
[English](README.md) | [Русский](README_ru.md)

Megabar突破寻找具有巨大实体和高成交量的K线，并用RSI均线过滤。看涨megabar做多，看跌megabar做空。

## 详情

- **入场条件**: 蜡烛实体和成交量超过各自均值乘以乘数，RSI均线高于/低于阈值。
- **多空方向**: 双向。
- **出场条件**: 止损或止盈。
- **止损**: 是。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Volume, Range, RSI
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
