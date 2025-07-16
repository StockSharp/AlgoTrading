# Adaptive EMA Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The **Adaptive EMA Breakout** strategy is built around Adaptive EMA breakout with trend confirmation.

Signals trigger when its indicators confirms breakout opportunities on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like Fast, Slow. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `Fast = 2`
  - `Slow = 30`
  - `Lookback = 10`
  - `StopMultiplier = 2m`
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
