# RSI Box (Pseudo Grid Bot)
[Русский](README_ru.md) | [中文](README_cn.md)

A grid-based strategy that derives price levels from RSI overbought and oversold signals. When RSI crosses an extreme, dynamic grid lines are recalculated from recent highs and lows. Trades occur when price breaks above or below the next grid level. Optional shorts are supported.

## Details

- **Entry Criteria**: Price crosses the next grid line after an RSI extreme.
- **Long/Short**: Long by default, shorts optional.
- **Exit Criteria**: Price crosses the opposite grid line.
- **Stops**: No.
- **Default Values**:
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `UseShorts` = false
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: RSI, Highest, Lowest
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
