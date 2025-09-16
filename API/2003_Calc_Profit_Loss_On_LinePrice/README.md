# Calc Profit Loss On LinePrice Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy for evaluating potential profit or loss at a user-defined price level.
It monitors finished candles and, when an open position exists, computes the hypothetical
profit or loss if the position were closed at the specified line price.
The result is written to the strategy log; the strategy itself does not place any orders.

## Details

- **Entry Criteria**: None (utility strategy)
- **Long/Short**: N/A
- **Exit Criteria**: N/A
- **Stops**: No
- **Default Values**:
  - `LinePrice` = 0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Utility
  - Direction: N/A
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Minimal
