# Futures Engulfing Candle Size Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades once per day when a candle's range exceeds a tick threshold within a selected time window. Direction follows the candle body and exits via take profit and stop loss.

## Details

- **Entry Criteria**: Candle range in ticks within trading session.
- **Long/Short**: Both.
- **Exit Criteria**: Take profit or stop loss.
- **Stops**: Take Profit & Stop Loss.
- **Default Values**:
  - `CandleType` = 1 minute
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
