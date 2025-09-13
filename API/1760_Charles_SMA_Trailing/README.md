# Charles SMA Trailing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades using a crossover of two Simple Moving Averages and optional trailing stop management. When the fast SMA crosses above the slow SMA a long position is opened. A short position is opened when the fast SMA crosses below the slow SMA. The strategy supports fixed stop-loss, take-profit, and a trailing stop that activates after a predefined profit threshold.

## Details

- **Entry Criteria**:
  - Fast SMA crosses above Slow SMA → open long.
  - Fast SMA crosses below Slow SMA → open short.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Reverse crossover.
  - Stop-loss or take-profit hit.
  - Trailing stop triggered after profit reaches `TrailStart` and trails by `TrailingAmount`.
- **Stops**:
  - `StopLoss` defines a fixed protective stop in price units.
  - `TakeProfit` defines a fixed profit target.
  - `TrailStart` and `TrailingAmount` control the trailing stop.
- **Default Values**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `StopLoss` = 0
  - `TakeProfit` = 25
  - `TrailStart` = 25
  - `TrailingAmount` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
