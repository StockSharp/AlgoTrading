# PEAD Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在盈利公告后出现正向EPS惊喜且跳空上涨时交易。
当价格在业绩公布后的第二天跳空上涨且近期表现良好时进场，
使用EMA追踪、固定止损/保本，以及持仓条数上限。

## 细节

- **入场条件**：EPS正向惊喜、业绩后跳空上涨，并且近期表现为正。
- **多空方向**：仅做多。
- **出场条件**：日线EMA下穿、固定止损/保本或持仓达到最大条数。
- **止损**：固定止损并可移动至保本。
- **默认值**：
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类别: Earnings
  - 方向: Long
  - 指标: EMA
  - 止损: 是
  - 复杂度: Intermediate
  - 时间框架: Daily
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: Medium
