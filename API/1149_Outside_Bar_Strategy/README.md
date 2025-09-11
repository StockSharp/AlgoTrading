# Outside Bar Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This strategy trades breakouts of outside bars. A bullish outside bar occurs when the current candle's high is above the previous high and its low is below the previous low. Orders are placed inside the bar with optional partial profit taking and breakeven stop movement.

## Details

- **Entry Criteria**: Outside bar with bullish or bearish classification.
- **Long/Short**: Both.
- **Exit Criteria**: Stop loss or take profit derived from bar range.
- **Stops**: Yes.
- **Default Values**:
  - `CandleType` = 5 minute
  - `EntryPercentage` = 0.5
  - `TpPercentage` = 1
  - `PartialRR` = 1
  - `PartialExitPercent` = 0.5
  - `StopLossOffset` = 10
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
