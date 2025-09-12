# IU Big Body Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters when the body of the current candle is several times larger than the average body size of the last 20 candles. A big bullish candle opens a long position, while a big bearish candle opens a short one. Positions are protected with an ATR-based trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: body > average body * BigBodyThreshold and close > open.
  - **Short**: body > average body * BigBodyThreshold and close < open.
- **Long/Short**: Both.
- **Exit Criteria**: ATR trailing stop.
- **Stops**: Trailing stop using ATR * AtrFactor.
- **Default Values**:
  - `BigBodyThreshold` = 4
  - `AtrLength` = 14
  - `AtrFactor` = 2
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

