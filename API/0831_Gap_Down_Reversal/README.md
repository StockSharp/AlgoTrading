# Gap Down Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Gap Down Reversal looks for bullish reversals after a gap down open.
When a new session opens below the previous low but closes above its open, it often traps sellers and signals a rebound.

The strategy enters long when this pattern appears and exits when price closes above the previous high.

## Details

- **Entry Criteria**: gap down reversal pattern
- **Long/Short**: Long only
- **Exit Criteria**: close above previous high
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1 day
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
