# Previous Day High Low Long Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long when price breaks above the previous day's high or low during a specified session and ADX indicates strengthening upward momentum.

## Details

- **Entry Criteria**:
  - **Long**: close crosses above previous day's high or low with rising ADX during the session.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - dynamic stop and profit targets or at session end.
- **Stops**: Trailing stop.
- **Default Values**:
  - `MaxProfit` = 150.
  - `MaxStopLoss` = 15.
  - `AdxLength` = 11.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: ADX
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
