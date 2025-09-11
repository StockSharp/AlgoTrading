# Hoffman Heiken Bias Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Hoffman Heiken Bias combines a group of moving averages with a Heikin Ashi net volume model to gauge trend direction. A long position is opened when the fast SMA rises above the fast EMA while all longer-term averages stay below it and the net volume regression is positive. Shorts trigger on the opposite conditions.

## Details

- **Entry Criteria**:
  - **Long**: `SMA(5) > EMA(18)` && all longer averages below `EMA(18)` && net volume regression > 0.
  - **Short**: `SMA(5) < EMA(18)` && all longer averages above `EMA(18)` && net volume regression < 0.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Fast SMA` = 5
  - `Fast EMA` = 18
  - `Net volume length` = 25
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, EMA, ATR, Linear Regression
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
