# Adaptive RSI Volume Filter The **Adaptive RSI Volume Filter** strategy is built around that trades based on Adaptive RSI with volume confirmation.

Signals trigger when its indicators confirms filtered entries on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like MinRsiPeriod, MaxRsiPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `MinRsiPeriod = 10`
  - `MaxRsiPeriod = 20`
  - `AtrPeriod = 14`
  - `VolumeLookback = 20`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: multiple indicators
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
