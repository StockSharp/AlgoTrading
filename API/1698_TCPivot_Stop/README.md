# TCPivot Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts through the daily pivot line. It calculates classical floor-trader pivot levels from the previous day's high, low and close. A long position is opened when the closing price crosses above the pivot. A short position is opened when the closing price crosses below the pivot.

After entry the system uses one of the support or resistance levels as both the profit target and stop loss. The level is selected by the **Target Level** parameter:

- **1** – uses `Support1`/`Resistance1`.
- **2** – uses `Support2`/`Resistance2`.
- **3** – uses `Support3`/`Resistance3`.

If **Intraday Only** is enabled every open position is closed at 23:00 platform time.

## Details

- **Entry Criteria**
  - **Long**: previous close ≤ pivot and current close > pivot.
  - **Short**: previous close ≥ pivot and current close < pivot.
- **Exit Criteria**
  - **Long**: close ≥ selected resistance level or close ≤ selected support level.
  - **Short**: close ≤ selected support level or close ≥ selected resistance level.
  - If *Intraday Only* is true, all positions are closed at 23:00.
- **Indicators**: classical pivot calculation only.
- **Timeframe**: configurable; default 5-minute candles.
- **Stops**: stop-loss and take-profit from chosen pivot level.
