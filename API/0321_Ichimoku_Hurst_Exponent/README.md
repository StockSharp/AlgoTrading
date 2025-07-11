# Ichimoku Hurst Exponent The **Ichimoku Hurst Exponent** strategy is built around Ichimoku Kinko Hyo indicator with Hurst exponent filter.

Signals trigger when Hurst confirms trend changes on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like TenkanPeriod, KijunPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Hurst, Exponent
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
