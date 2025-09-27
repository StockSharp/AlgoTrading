# I Gap Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **I Gap Strategy** replicates the MetaTrader "i-GAP" expert advisor. It monitors the price gap between the previous candle's close and the current candle's open. A downward opening gap exceeding a specified number of price steps can trigger a long entry and optionally close existing short positions. An upward gap works the same way for shorts.

## Details
- **Entry Criteria**: Opening gap between consecutive candles exceeds the configured size.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite gap signal.
- **Stops**: No fixed stop loss or take profit.
- **Default Values**:
  - `CandleType` = 1 hour
  - `GapSize` = 5
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filters**:
  - Category: Gap
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
