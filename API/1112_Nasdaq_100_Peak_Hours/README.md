# NASDAQ 100 Peak Hours Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades NASDAQ 100 only during the first two and last hours of the session. It uses EMA trend confirmation, RSI, ATR and VWAP filters with ATR based trailing and break-even stops.

## Details

- **Entry Criteria**:
  - **Long**: Price above short EMA, short EMA above long EMA, both EMAs rising, RSI above 50 and price above VWAP during peak session hours.
  - **Short**: Opposite conditions.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - ATR based trailing stop or break-even stop.
  - Time based exit after configurable number of bars or EMA trend reversal.
- **Stops**: ATR trailing with break-even.
- **Default Values**:
  - `Long EMA` = 21
  - `Short EMA` = 9
  - `RSI` = 14
  - `ATR` = 14
  - `Trail ATR Mult` = 1.5
  - `Initial SL Mult` = 0.5
  - `Break-even ATR Mult` = 1.5
  - `Time Exit Bars` = 20
- **Filters**:
  - Category: Intraday
  - Direction: Both
  - Indicators: EMA, RSI, ATR, VWAP
  - Stops: Trailing
  - Complexity: Advanced
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
