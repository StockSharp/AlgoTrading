# Times Direction Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This time-based strategy opens a single long or short position during a predefined window and closes it during another window. The entry direction is configurable and the system monitors optional stop-loss and take-profit levels. The approach relies solely on finished candles without using indicators.

## Details

- **Entry Criteria**:
  - When current candle time is within `[OpenTime, OpenTime + TradeInterval)` and no position is open, enter in the configured direction.
- **Exit Criteria**:
  - Close the position when time is within `[CloseTime, CloseTime + TradeInterval)`.
  - Additionally exit if stop-loss or take-profit levels are reached.
- **Long/Short**: Configurable.
- **Stops**: Stop-loss and take-profit in price units relative to entry price.
- **Default Values**:
  - `Trade` = Sell.
  - `OpenTime` = 1970-01-01 00:00.
  - `CloseTime` = 3000-01-01 00:00.
  - `TradeInterval` = 1 minute.
  - `StopLoss` = 1000.
  - `TakeProfit` = 2000.
  - `Volume` = 0.1.
- **Filters**:
  - Category: Time based
  - Direction: Single
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Short-term
