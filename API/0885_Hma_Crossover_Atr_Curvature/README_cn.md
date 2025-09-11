# HMA Crossover ATR Curvature
[English](README.md) | [Русский](README_ru.md)

HMA Crossover ATR Curvature 是一种趋势跟随策略，结合快慢 Hull 移动平均线交叉和曲率过滤。仓位大小基于 ATR 和风险百分比，使用基于 ATR 的跟踪止损保护。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：快 HMA 上穿慢 HMA 且曲率大于阈值。
  - **空头**：快 HMA 下穿慢 HMA 且曲率小于负阈值。
- **出场条件**：ATR 跟踪止损。
- **止损**：ATR 跟踪止损。
- **默认值**：
  - `FastLength` = 15
  - `SlowLength` = 34
  - `AtrLength` = 14
  - `RiskPercent` = 1
  - `AtrMultiplier` = 1.5
  - `TrailMultiplier` = 1
  - `CurvatureThreshold` = 0
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多头 & 空头
  - 指标：HMA, ATR
  - 复杂度：低
  - 风险等级：中等
