# 350 CCI Put Call Ratio Divergence
The **CCI Put Call Ratio Divergence** strategy is built around CCI Put Call Ratio Divergence.

Signals trigger when Divergence confirms divergence setups on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like CciPeriod, AtrMultiplier. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `CciPeriod = 20`
  - `AtrMultiplier = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Divergence
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
