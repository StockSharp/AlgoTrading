# Supertrend Put Call Ratio
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Supertrend Put Call Ratio** strategy is built around Supertrend Put Call Ratio.

Signals trigger when its indicators confirms trend changes on intraday (15m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like Period, Multiplier. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `Period = 10`
  - `Multiplier = 3m`
  - `PCRPeriod = 20`
  - `PCRMultiplier = 2m`
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
