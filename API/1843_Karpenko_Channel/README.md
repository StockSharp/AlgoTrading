# Karpenko Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Karpenko Channel strategy builds a dynamic price channel using two moving averages. The base line is an average of closing prices, while the upper and lower bounds are derived from the average high-low range scaled by the golden ratio 1.618. The channel expands until it envelopes the current bar.

A signal to go long appears when the upper bound, previously above the base line, crosses below it. A short signal arises when the upper bound crosses above the base line after staying below. Existing positions in the opposite direction are closed when the regime changes.

Only finished candles are processed. Fixed stop-loss and take-profit levels protect each trade.

## Details

- **Entry Criteria:**
  - **Long:** Previous upper bound above base line and current value below or equal to it.
  - **Short:** Previous upper bound below base line and current value above or equal to it.
- **Exit Criteria:**
  - Close long when previous upper bound was below the base line.
  - Close short when previous upper bound was above the base line.
- **Stops:** Fixed stop-loss and take-profit distances in price units.
- **Default Values:**
  - `Base MA` = 144
  - `History` = 500
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4 hour
- **Filters:**
  - Category: Trend following
  - Direction: Both
  - Indicators: Custom
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
