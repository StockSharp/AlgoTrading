# IBS Internal Bar Strength
[English](README.md) | [Русский](README_ru.md)

IBS Internal Bar Strength 是一种均值回归策略，通过前一根K线在其高低区间中的收盘位置来识别超卖或超买状态。可选的EMA过滤器用于顺势交易，只有当价格相对上次入场达到最小百分比偏移时才允许加仓。当IBS穿越相反阈值或持仓时间超过限制时平仓。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：IBS 低于入场阈值，满足 EMA 条件且方向允许。
  - **空头**：IBS 高于出场阈值，满足 EMA 条件且方向允许。
- **出场条件**：IBS 穿越相反阈值或达到最长持仓时间。
- **止损**：时间退出。
- **默认值**：
  - `IbsEntryThreshold` = 0.09
  - `IbsExitThreshold` = 0.985
  - `EmaPeriod` = 220
  - `MinEntryPct` = 0
  - `MaxTradeDuration` = 14
- **过滤器**：
  - 类别：均值回归
  - 方向：多头 & 空头
  - 指标：IBS, EMA
  - 复杂度：低
  - 风险等级：中等
