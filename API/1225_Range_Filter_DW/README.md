# Range Filter DW Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements an ATR-based range filter similar to Donovan Wall's Range Filter. The filter ignores minor price movements by moving only when price exceeds a volatility-based range. A long position is opened when the close is above the upper band, while a short position is opened when the close is below the lower band.

## Details

- **Entry Criteria**:
  - **Long**: Close above the upper band.
  - **Short**: Close below the lower band.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite band breakout.
- **Stops**: No.
- **Default Values**:
  - `RangePeriod` = 14
  - `RangeMultiplier` = 2.618
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ATR
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
