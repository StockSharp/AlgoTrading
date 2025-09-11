# Dual Keltner Channels
[Русский](README_ru.md) | [中文](README_cn.md)

The **Dual Keltner Channels** strategy uses two Keltner Channels with different multipliers to detect breakouts.
A trade is opened when price pierces the outer band and then returns through the inner band.
Stops and targets are managed with fixed percentages.

## Details
- **Entry Criteria**: Price crosses the outer Keltner band and re-crosses the inner band in the same direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss, take-profit, or opposite signal.
- **Stops**: Yes, percentage-based.
- **Default Values**:
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Keltner
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
