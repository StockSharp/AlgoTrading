# 345 VWAP Behavioral Bias Filter
The **VWAP Behavioral Bias Filter** strategy is built around VWAP Behavioral Bias Filter.

Signals trigger when Behavioral confirms filtered entries on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like BiasThreshold, BiasWindowSize. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `BiasThreshold = 0.5m`
  - `BiasWindowSize = 20`
  - `StopLoss = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Behavioral, Bias
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
