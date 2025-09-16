# Directed Movement Candle 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于K线收盘价计算的相对强弱指数(RSI)。当RSI离开中性区并突破自定义阈值时，策略顺势开仓并在反向信号出现时平仓。

## 详情

- **指标**：相对强弱指数，可调 `RsiPeriod`。
- **HighLevel**：表示多头动能的RSI值。
- **MiddleLevel**：用于参考的中间水平。
- **LowLevel**：表示空头动能的RSI值。
- **入场**：
  - RSI从下方突破 `HighLevel` 时做多。
  - RSI从上方跌破 `LowLevel` 时做空。
- **出场**：出现相反信号时平掉现有仓位再开新仓。
- **多空**：支持双向交易。
- **止损**：默认未使用。
- **默认参数**：
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = 5分钟周期
