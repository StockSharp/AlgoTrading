# MOC Delta MOO Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes buy and sell volume delta during the 14:50–14:55 session and trades at 08:30 if the delta percentage exceeds a threshold relative to the day's volume. It uses SMA filters on the open price and attaches tick-based stop loss and take profit.

## Details

- **Entry Criteria:**
  - **Long:** 08:30, MOC delta % above threshold, open above SMA15 and SMA30.
  - **Short:** 08:30, MOC delta % below negative threshold, open below SMA15 and SMA30.
- **Exit Criteria:**
  - **Stops:** Take-profit and stop-loss in ticks.
  - **Time-based:** Close all positions at 14:50.
- **Default Values:**
  - `DeltaThreshold` = 2
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 10
