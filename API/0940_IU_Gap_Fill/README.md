# IU Gap Fill Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

IU Gap Fill Strategy enters trades when the price gaps away from the previous session close and then fills that gap. A long position opens after a gap up that dips below the prior close and closes back above it. A short position opens after a gap down that rallies above the prior close and closes back below. An ATR-based trailing stop manages exits.

## Details
- **Data**: Candles from a user-defined timeframe.
- **Entry Criteria**:
  - **Long**: Gap up of at least `GapPercent` and price crosses above the previous session close.
  - **Short**: Gap down of at least `GapPercent` and price crosses below the previous session close.
- **Exit Criteria**: ATR trailing stop.
- **Stops**: ATR `AtrLength` * `AtrFactor` trailing level.
- **Default Values**:
  - `CandleType` = 1m
  - `GapPercent` = 0.2
  - `AtrLength` = 14
  - `AtrFactor` = 2
- **Filters**:
  - Category: Gap
  - Direction: Long & Short
  - Indicators: ATR
  - Complexity: Low
  - Risk level: Medium
