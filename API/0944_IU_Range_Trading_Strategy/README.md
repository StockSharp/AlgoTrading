# IU Range Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The IU Range Trading Strategy identifies consolidation zones where the price range over a lookback period stays within an ATR multiplier. Breakout trades trigger when price exceeds the range boundaries. Positions are protected by an ATR-based trailing stop that moves with favorable price action.

## Details

- **Entry Criteria**: Price breaks above or below a narrow ATR-defined range.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based trailing stop.
- **Stops**: Yes.
- **Default Values**:
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, Highest, Lowest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
