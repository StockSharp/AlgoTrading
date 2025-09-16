# MaRsi Trigger Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines fast and slow exponential moving averages (EMA) with RSI to detect trend reversals.
When the fast EMA and fast RSI are both above their slow counterparts, it treats the market as bullish and opens a long position.
When both are below, it opens a short position. Parameters allow enabling or disabling long and short entries or exits.

## Details

- **Entry Criteria**:
  - **Long**: fast EMA > slow EMA AND fast RSI > slow RSI with previous trend bearish.
  - **Short**: fast EMA < slow EMA AND fast RSI < slow RSI with previous trend bullish.
- **Exit Criteria**:
  - **Long**: trend turns bearish and long exits are allowed.
  - **Short**: trend turns bullish and short exits are allowed.
- **Indicators**: EMA, RSI.
- **Stops**: Not included.
- **Timeframe**: 4-hour candles by default.
- **Parameters**:
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
