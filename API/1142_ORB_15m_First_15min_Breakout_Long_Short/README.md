# ORB 15m – First 15min Breakout (Long/Short)
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters at the close of the first 15‑minute bar after the session open in Stockholm time. A bullish first bar triggers a long trade, a bearish bar triggers a short. Position size is calculated from risk percentage and the distance to the stop.

## Details

- **Entry Criteria**: trade on the first 15-minute bar after session open; long if the bar closes above its open, short if below.
- **Exit Criteria**: stop-loss at the opposite extreme of the reference bar; optional take profit at `RMultiple` times risk or otherwise at session end.
- **Long/Short**: both.
- **Stops**: yes.
- **Default Values**:
  - `RiskPct = 1`
  - `TpTenR = true`
  - `RMultiple = 10`
  - `SessionOpenHour = 15`
  - `SessionOpenMinute = 30`
  - `SessionEndHour = 22`
  - `SessionEndMinute = 0`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
