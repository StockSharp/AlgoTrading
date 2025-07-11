# VWAP Stochastic Divergence
The **VWAP Stochastic Divergence** strategy is built around combining VWAP with ADX trend strength indicator.

Signals trigger when Stochastic confirms divergence setups on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like AdxPeriod, AdxThreshold. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `AdxExitThreshold = 20m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Stochastic, Divergence
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
