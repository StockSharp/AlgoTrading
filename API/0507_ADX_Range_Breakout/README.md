# ADX Range Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long positions when the close breaks above the highest close of a lookback period while the ADX remains below a specified threshold, indicating a quiet market. Trading is limited to a defined session and a maximum number of trades per day. A fixed stop-loss in price units protects every position.

## Details

- **Entry Criteria**: `Close >= previous highest close` and `ADX < threshold` within session
- **Long/Short**: Long only
- **Exit Criteria**: Stop-loss or session end
- **Stops**: Yes
- **Default Values**:
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: ADX
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
