# 339 MACD Sentiment Filter
The **MACD Sentiment Filter** strategy is built around MACD Sentiment Filter.

Signals trigger when its indicators confirms filtered entries on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like MacdFast, MacdSlow. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `Threshold = 0.5m`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: multiple indicators
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
