# Opening Range Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy defines an opening range and trades breakouts above or below it. After the opening range window closes, if the width exceeds a percentage of the close price, stop orders are prepared at the range boundaries. Positions use a stop loss and profit target based on the range size. Optionally only one trade per day is taken, and losing trades may reverse. All positions are closed at the end of the session.

## Details

- **Entry Criteria**:
  - **Long**: price breaks above the opening range high.
  - **Short**: price breaks below the opening range low.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Stop loss or profit target based on range.
  - End-of-day flat.
- **Stops**: Yes.
- **Default Values**:
  - `Opening range` = 09:30–10:15.
  - `Day end` = 15:45.
  - `MinRangePercent` = 0.35.
  - `RewardRisk` = 1.1.
  - `Retrace` = 0.5.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
