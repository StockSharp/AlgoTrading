# CP Strat ORB
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of the New York opening range (9:30-9:45) with a retest. It enters long after price breaks above the range high and closes back above it, and enters short after price breaks below the range low and closes back below it. Exits use fixed stop-loss and take-profit levels.

## Details

- **Entry Criteria**: Breakout of the NY opening range followed by a retest and close beyond the range limit.
- **Long/Short**: Both.
- **Exit Criteria**: Fixed take-profit or stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `MinRangePoints` = 60m
  - `StopPoints` = 20m
  - `TakePoints` = 60m
  - `MaxTradesPerSession` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
