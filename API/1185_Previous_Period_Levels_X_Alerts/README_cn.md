# 前期水平 - X Alerts
[English](README.md) | [Русский](README_ru.md)

该策略跟踪更高时间框架上一周期的开盘价、最高价、最低价和收盘价。在基础时间框架上计算移动平均线，当其与这些水平交叉时记录日志，与 TradingView 指标 "Previous Period Levels - X Alerts" 类似。

## 细节

- **入场条件**：无，仅记录 SMA 与水平的交叉。
- **多空方向**：无。
- **出场条件**：无。
- **止损**：无。
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `ReferenceCandleType` = TimeSpan.FromHours(1)
  - `SmaLength` = 3
  - `UseOpen` = true
  - `UseHigh` = true
  - `UseLow` = true
  - `UseClose` = true
- **筛选**：
  - 类别: Levels
  - 方向: Neutral
  - 指标: SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 低
