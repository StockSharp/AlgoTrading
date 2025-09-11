# Gold Pullback Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Gold Pullback Strategy combines EMA trend direction with MACD and TDI filters. Long trades trigger when price touches the 21‑period EMA during an uptrend and both MACD and TDI are bullish. Short trades occur on pullbacks to the EMA21 in downtrends with bearish MACD and TDI. Each position uses a 1:1 take profit and stop loss based on the signal candle plus an offset.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: EMA14 above EMA60, candle touches EMA21, MACD line above signal line, TDI MA above TDI signal and RSI above 50.
  - **Short**: EMA14 below EMA60, candle touches EMA21, MACD line below signal line, TDI MA below TDI signal and RSI below 50.
- **Exit Criteria**: Stop loss or take profit hit at equal distance from entry with an added offset.
- **Stops**: `Offset` = 0.1 applied to candle low/high.
- **Default Values**:
  - `EmaFastLength` = 14
  - `EmaSlowLength` = 60
  - `EmaPullbackLength` = 21
  - `SlOffset` = 0.1
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: EMA, MACD, RSI, SMA
  - Complexity: Medium
  - Risk level: Medium
