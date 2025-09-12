# ICT Master Suite Trading IQ Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The ICT Master Suite strategy trades breakouts of the daily session high and low. When price closes above the session high the strategy enters a long position; when price closes below the session low it enters a short position. Positions are managed with an ATR-based trailing stop.

## Details

- **Entry Criteria**:
  - Price closes above current session high (long).
  - Price closes below current session low (short).
- **Long/Short**: Long & Short.
- **Exit Criteria**:
  - ATR-based trailing stop.
- **Stops**: ATR trailing stop.
- **Default Values**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `AllowLong` = true
  - `AllowShort` = true
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
