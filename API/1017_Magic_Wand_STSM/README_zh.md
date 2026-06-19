# Magic Wand STSM 策略
[English](README.md) | [Русский](README_ru.md)

该趋势策略使用 Supertrend 指标并结合 200 期 SMA 过滤。按 Supertrend 方向交易，使用其作为止损，并根据设定的风险回报比计算止盈。

## 细节

- **入场条件**：
  - **多头**：Supertrend 位于价格下方且收盘价高于 SMA200。
  - **空头**：Supertrend 位于价格上方且收盘价低于 SMA200。
- **多空方向**：双向。
- **出场条件**：
  - 止盈 `entry ± (entry - Supertrend) * RiskReward`。
  - 止损在 Supertrend 处。
- **止损**：有。
- **默认值**：
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **过滤器**：
  - 类别: Trend Following
  - 方向: 双向
  - 指标: 多个
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
