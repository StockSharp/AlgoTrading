# EMA 10/55/200 Long-Only MTF Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens long positions when EMA crossovers on the 4-hour chart align with bullish trends on daily and weekly charts.

## Details

- **Entry Criteria**:
  - `EMA10` crosses above `EMA55` with the candle high above `EMA55`, or `EMA55` crosses above `EMA200`, or `EMA10` crosses above `EMA500`.
  - Daily `EMA55` is above `EMA200` and weekly `EMA55` is above `EMA200`.
- **Exit Criteria**:
  - `EMA10` crosses below `EMA200` or `EMA500`.
  - Price falls to the stop loss level.
- **Parameters**:
  - `EMA 10 Length` = 10
  - `EMA 55 Length` = 55
  - `EMA 200 Length` = 200
  - `EMA 500 Length` = 500
  - `Stop Loss %` = 5
