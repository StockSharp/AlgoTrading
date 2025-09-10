# Bitcoin Momentum
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum strategy for Bitcoin that trades only when price is above a higher timeframe EMA and avoids caution conditions. An ATR-based trailing stop protects gains.

## Details

- **Entry Criteria**: Price above weekly EMA and no caution condition.
- **Long/Short**: Long only.
- **Exit Criteria**: Price below trailing stop or weekly EMA.
- **Stops**: ATR-based trailing stop.
- **Default Values**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: EMA, ATR, Highest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
