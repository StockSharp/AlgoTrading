# Max Pain 策略
[English](README.md) | [Русский](README_ru.md)

当成交量和价格变动超过可配置阈值且VIX指数低于设定水平时，该策略开多单。进场时根据波动率设置止损，并在固定的周期数后平仓。

## 细节

- **入场条件**：
  - **多头**：成交量大于平均成交量 × `VolumeMultiplier`，且价格变动大于前一收盘价 × `PriceChangeMultiplier`，同时VIX低于 `VixThreshold`。
- **多空方向**：仅多头。
- **出场条件**：
  - 在入场价下方 `StopLossMultiplier` × 波动率 处设置止损。
  - 在 `HoldPeriods` 根K线后平仓。
- **止损**：有。
- **默认值**：
  - `LookbackPeriod` = 70。
  - `VolumeMultiplier` = 1。
  - `PriceChangeMultiplier` = 0.029。
  - `StopLossMultiplier` = 2.4。
  - `VixThreshold` = 44。
  - `HoldPeriods` = 8。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromDays(1).TimeFrame().
- **筛选**：
  - 类型: 突破
  - 方向: 仅多头
  - 指标: 成交量、价格行为、波动率
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
