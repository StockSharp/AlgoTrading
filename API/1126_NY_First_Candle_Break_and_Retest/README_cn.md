# 纽约首根K线突破回测策略
[English](README.md) | [Русский](README_ru.md)

本策略在纽约交易时段记录首根K线的高低点，当价格突破并回测该水平后入场。使用ATR设定止损与盈亏比目标，可选EMA趋势过滤和追踪止损。

## 细节

- **入场条件**：价格突破首根K线的高点或低点并在`RetestThreshold` ATR内回测。
- **多空方向**：均可。
- **出场条件**：ATR止损与`RewardRiskRatio`目标，可启用追踪止损。
- **止损**：`AtrMultiplier` × ATR。
- **默认参数**：
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **筛选**：
  - 分类: Breakout
  - 方向: 双向
  - 指标: ATR, EMA
  - 止损: ATR
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 是
  - 神经网络: 否
  - 背离: 否
  - 风险: 中
