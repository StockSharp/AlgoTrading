# Traling Line Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy manages open positions with a dynamic trailing line derived from the previous four-hour candle.
For long positions the line is placed below the candle's low, for short positions above the candle's high.
When the market closes beyond this line the corresponding position is exited.
The system does not generate entries and is intended to protect manually opened trades.

## Details

- **Entry Criteria**: None – positions must be opened elsewhere.
- **Long Protection**: `Stop = Low(H4) - StopLevel * PriceStep`.
- **Short Protection**: `Stop = High(H4) + StopLevel * PriceStep`.
- **Long/Short**: Both.
- **Exit Criteria**: Close price crosses the trailing line.
- **Stops**: Trailing stop only.
- **Default Values**:
  - `StopLevel` = 70 price steps.
  - `CandleType` = 4-hour candles.
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Long-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
