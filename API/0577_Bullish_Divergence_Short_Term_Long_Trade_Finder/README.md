# Bullish Divergence Short-term Long Trade Finder

This strategy searches for bullish divergences between price and RSI. When price makes a lower low but RSI forms a higher low within a specified pivot range and the hourly RSI is below 40, the strategy enters a long position. The position is closed when RSI rises above a threshold, a bearish divergence appears, or the stop loss is hit.

- **Entry Conditions**:
  - Current low is below the previous pivot low price.
  - RSI forms a higher low below `RsiBullConditionMin` and the previous pivot occurs within 5–50 bars.
  - Hourly RSI is below `RsiHourEntryThreshold`.
  - Close price is below the previous pivot low price.
- **Exit Conditions**:
  - RSI crosses above `SellWhenRsi`.
  - Bearish divergence: price makes a higher high while RSI makes a lower high.
  - Stop loss activated via `StartProtection` at `StopLossPercent`.
- **Indicators**: RSI.
