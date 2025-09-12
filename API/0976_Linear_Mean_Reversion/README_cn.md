# Linear Mean Reversion Strategy
[English](README.md) | [Русский](README_ru.md)

Linear Mean Reversion Strategy 利用价格相对于均值的 z 分数进行均值回归交易，并使用固定点数止损。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：z-score < -EntryThreshold。
  - **空头**：z-score > EntryThreshold。
- **出场条件**：z-score 回到零附近（多头 z-score > -ExitThreshold，空头 z-score < ExitThreshold）。
- **止损**：固定点数止损。
- **默认值**：
  - `HalfLife` = 14
  - `Scale` = 1
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.2
  - `StopLossPoints` = 50
- **过滤器**：
  - 类别：均值回归
  - 方向：多头 & 空头
  - 指标：SMA, StandardDeviation
  - 止损：是
  - 复杂度：低
  - 风险等级：中等
