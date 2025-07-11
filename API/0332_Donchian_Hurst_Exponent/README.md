# Donchian Hurst Exponent

The **Donchian Hurst Exponent** strategy is built around that trades based on Donchian Channel breakouts with Hurst Exponent filter.

Signals trigger when Donchian confirms trend changes on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like DonchianPeriod, HurstPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `DonchianPeriod = 20`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `StopLossPercent = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Donchian, Hurst, Exponent
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
