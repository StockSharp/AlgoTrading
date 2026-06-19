# Bullish Divergence Short-term Long Trade Finder

该策略寻找价格与RSI之间的看涨背离。当价格创新低而RSI在指定的枢轴范围内形成更高的低点，并且小时RSI低于40时，策略做多。当RSI超过阈值、出现看跌背离或触发止损时平仓。

- **入场条件**：
  - 当前最低价低于前一个枢轴低点价格。
  - RSI 在 `RsiBullConditionMin` 以下形成更高的低点，且前一个枢轴在 5–50 根K线内出现。
  - 小时 RSI 低于 `RsiHourEntryThreshold`。
  - 收盘价低于前一个枢轴低点价格。
- **出场条件**：
  - RSI 上穿 `SellWhenRsi`。
  - 看跌背离：价格创新高而 RSI 形成更低的高点。
  - `StartProtection` 在 `StopLossPercent` 处触发止损。
- **指标**：RSI。
