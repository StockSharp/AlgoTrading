# IMA专家策略
[English](README.md) | [Русский](README_ru.md)

该策略利用价格相对于其移动平均的相对速度进行交易。
指标 `Close / SMA - 1` 在连续两根K线之间比较。指标快速上升开多头，快速下降开空头。

## 细节

- **入场条件**：
  - 多头：`(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - 空头：`(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **出场条件**：反向信号
- **头寸大小**：`RiskLevel` 与 `StopLossTicks` 决定交易量，并受 `MaxVolume` 限制
- **多空**：双向
- **止损**：无
- **默认值**：
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
