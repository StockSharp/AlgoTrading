# Averaged Stoch & WPR 策略
[English](README.md) | [Русский](README_ru.md)

该策略将随机指标 (Stochastic) 与威廉指标 (Williams %R) 结合，用于识别市场的极端超买和超卖状态。
当随机指标低于 0.1 且 Williams %R 低于 -90 时开多单，表示市场严重超卖。
当随机指标高于 99.9 且 Williams %R 高于 -5 时开空单，表示市场严重超买。

策略适用于所选蜡烛类型支持的任何品种和周期，可进行多空交易，并提供可选的百分比止损以控制风险。

## 细节

- **入场条件**：
  - **做多**：Stochastic < 0.1 且 Williams %R < -90。
  - **做空**：Stochastic > 99.9 且 Williams %R > -5。
- **多空方向**：双向。
- **出场条件**：反向信号或触发止损。
- **止损**：可选百分比止损。
- **指标**：
  - 随机指标（默认周期 26）。
  - Williams %R（默认周期 26）。

## 参数

- `StochPeriod` – 随机指标周期。
- `WprPeriod` – Williams %R 周期。
- `StopLossPercent` – 百分比止损大小。
- `CandleType` – 用于计算指标的蜡烛类型。
