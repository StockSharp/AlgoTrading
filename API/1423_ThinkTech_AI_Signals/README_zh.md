# ThinkTech AI Signals 策略
[English](README.md) | [Русский](README_ru.md)

该策略在每天第一个15分钟K线突破时入场，使用ATR设置止损和止盈，可选趋势和RSI过滤。

## 细节

- **入场条件**：
  - **多头**：价格突破首根K线高点且通过趋势和RSI过滤。
  - **空头**：价格跌破首根K线低点且通过趋势和RSI过滤。
- **多/空**：双向。
- **出场条件**：
  - 触及止盈或止损。
- **止损**：ATR计算。
- **默认值**：
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
