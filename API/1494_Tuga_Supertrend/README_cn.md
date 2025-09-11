# Tuga Supertrend
[English](README.md) | [Русский](README_ru.md)

Tuga Supertrend 是一种基于 SuperTrend 指标的纯多策略。当 SuperTrend 方向由上转下时开多头，在方向由下转上时平仓。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：在指定时间窗口内 SuperTrend 方向由上转下。
- **出场条件**：SuperTrend 方向由下转上。
- **止损**：无。
- **默认值**：
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多头
  - 指标：SuperTrend, ATR
  - 复杂度：低
  - 风险等级：中等
