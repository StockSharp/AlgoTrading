# MBKAsctrend3 策略
[English](README.md) | [Русский](README_ru.md)

MBKAsctrend3 策略使用三个不同周期的 Williams %R 振荡器。它们的加权组合用于确定市场趋势。当加权值高于 67+Swing 且长期振荡器高于 50-AverageSwing 时开多单；当值低于 33-Swing 且长期振荡器低于 50+AverageSwing 时开空单。头寸通过以点表示的可配置止损和止盈进行保护。

## 细节
- **入场条件**：
  - **做多**：加权 WPR > 67+Swing 且长期 WPR > 50-AverageSwing。
  - **做空**：加权 WPR < 33-Swing 且长期 WPR < 50+AverageSwing。
- **做多/做空**：均可。
- **出场条件**：相反信号或保护水平。
- **止损**：绝对止损和止盈。
- **过滤器**：无。

## 参数
- `WprLength1`、`WprLength2`、`WprLength3` – 三个 Williams %R 指标的周期。
- `Swing` – 上下阈值的偏移。
- `AverageSwing` – 基于长期振荡器的附加偏移。
- `Weight1`、`Weight2`、`Weight3` – 每个指标的权重。
- `StopLoss`、`TakeProfit` – 以点表示的保护水平。
- `CandleType` – K线周期，默认为 4 小时。
