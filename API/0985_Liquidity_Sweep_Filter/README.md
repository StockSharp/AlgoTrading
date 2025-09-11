# Liquidity Sweep Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This trend-following strategy uses Bollinger bands to detect market direction and monitors volume for potential liquidity sweeps. A position is opened when the trend flips bullish or bearish depending on the selected trade mode.

## Details

- **Entry Criteria**:
  - **Long**: Trend turns bullish and mode allows long trades.
  - **Short**: Trend turns bearish and mode allows short trades.
- **Long/Short**: Configurable via trade mode.
- **Exit Criteria**:
  - **Long**: Trend turns bearish or mode forbids long.
  - **Short**: Trend turns bullish or mode forbids short.
- **Stops**: None.
- **Default Values**:
  - `Length` = 12.
  - `Multiplier` = 2.0.
  - `Major Sweep Threshold` = 50.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

