# ADX Volume Breakout The **ADX Volume Breakout** strategy is built around ADX with Volume Breakout.

Signals trigger when its indicators confirms breakout opportunities on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like AdxPeriod, AdxThreshold. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `AdxPeriod = 14`
  - `AdxThreshold = 25m`
  - `VolumeAvgPeriod = 20`
  - `VolumeThresholdFactor = 2.0m`
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
