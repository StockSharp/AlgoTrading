# Pure Price Action Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified price action strategy detecting Break of Structure (BOS) and Market Structure Shift (MSS) from recent highs and lows.

The strategy enters long on BOS and short on MSS with fixed stop-loss and take-profit percentages.

## Details

- **Entry Criteria**: BOS for long, MSS for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Fixed percentage.
- **Default Values**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Price Action
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
