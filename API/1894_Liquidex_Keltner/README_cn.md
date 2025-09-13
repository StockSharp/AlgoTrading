# Liquidex Keltner
[English](README.md) | [Русский](README_ru.md)

**Liquidex Keltner** 策略结合移动平均线和 Keltner 通道来交易突破。
仅在指定的时间段内交易，并可选择使用 RSI 方向过滤。
止损和止盈通过固定百分比进行管理。

## 详情
- **入场条件**：
  - 价格突破上轨并收于移动平均线上方。
  - 价格突破下轨并收于移动平均线下方。
  - K线实体必须大于 `RangeFilter`。
  - 启用 `UseRsiFilter` 时，多头需 RSI > 50，空头需 RSI < 50。
  - 当前时间需在 `EntryHourFrom` 与 `EntryHourTo` 之间，且周五需早于 `FridayEndHour`。
- **多头/空头**：双向。
- **出场条件**：止损或止盈。
- **止损**：是，通过 `StartProtection` 的百分比。
- **默认值**：
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **过滤器**：
  - 类别: 突破
  - 方向: 双向
  - 指标: MA, Keltner, RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (15m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
