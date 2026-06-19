# EMA Moving Away
[English](README.md) | [Русский](README_ru.md)

EMA Moving Away 策略监测价格与指数移动平均线 (EMA) 的偏离程度。
当连续的蜡烛使价格偏离 EMA 达到设定百分比时，策略预期价格将回归均值。

该策略只做多：若持续的下跌使价格低于 EMA 且偏离超过 `MovingAwayPercent`，
则开多仓。可结合实体大小和连续蜡烛过滤以确认运动不是噪音。
`StopLossPercent` 参数提供百分比止损。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: 收盘价低于 EMA 且偏离达到 `MovingAwayPercent`，并满足体积/连续过滤。
  - **空头**: 不使用。
- **离场条件**: 价格返回 EMA 或触发止损。
- **止损**: 百分比止损 (`StopLossPercent`)。
- **默认参数**:
  - `EmaLength` = 55
  - `MovingAwayPercent` = 2.0
  - `StopLossPercent` = 2.0
- **过滤器**:
  - 类型: 均值回归
  - 方向: 仅多头
  - 指标: EMA
  - 复杂度: 中等
  - 风险级别: 中等
