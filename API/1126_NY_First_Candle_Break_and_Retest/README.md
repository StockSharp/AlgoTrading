# NY First Candle Break and Retest Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of the first New York session candle with retest confirmation. Uses ATR for stop placement and reward-to-risk targets with optional EMA trend filter and trailing stop.

## Details

- **Entry Criteria**: Break of the first session candle high or low followed by a retest within `RetestThreshold` ATR.
- **Long/Short**: Both.
- **Exit Criteria**: ATR-based stop and `RewardRiskRatio` target. Optional trailing stop.
- **Stops**: `AtrMultiplier` * ATR.
- **Default Values**:
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, EMA
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
