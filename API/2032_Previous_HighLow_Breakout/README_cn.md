# Previous High Low Breakout 策略
[English](README.md) | [Русский](README_ru.md)

该策略监控所选周期上一根K线的最高价和最低价。当新的K线收盘价突破前高时开多单，收盘价跌破前低时开空单。使用固定的止盈和带追踪的止损来控制风险。

该方法旨在捕捉整理后的强趋势行情。追踪止损在价格朝有利方向移动时保持风险可控。

## 细节

- **入场条件**：
  - 多头：`Close > PreviousHigh`
  - 空头：`Close < PreviousLow`
- **多空**：双向
- **出场条件**：
  - 触发止损或止盈
- **止损**：通过 `StopLoss` 与 `TakeProfit` 设置的绝对值追踪止损
- **默认值**：
  - `StopLoss` = 50m
  - `TakeProfit` = 1000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：无
  - 止损：有（追踪）
  - 复杂度：初级
  - 周期：长周期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

