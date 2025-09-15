# Limits Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Places symmetric limit orders around each candle's open price and protects positions with stop-loss, take-profit and optional trailing.

## Details

- **Entry**:
  - Buy limit at `Open - StopOrderDistance * PriceStep` if long trading enabled.
  - Sell limit at `Open + StopOrderDistance * PriceStep` if short trading enabled.
- **Exit**: Market close on stop-loss, take-profit or trailing stop trigger.
- **Long/Short**: Both.
- **Stops**: Fixed stop-loss with trailing option.
- **Default values**:
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1 minute
- **Session**: Trades only between `StartTime` and `EndTime`.
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
