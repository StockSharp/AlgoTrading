# Instant Execution Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy immediately enters a single position on the first completed candle and manages it with simple profit and risk rules. The position direction is selectable through parameters. Once a trade is opened the algorithm tracks profit and loss and can trail the price to protect gains.

The logic reproduces the behaviour of the original MQL script that allowed instant execution of market orders with optional take-profit, stop-loss and trailing stop values.

## Details

- **Entry Criteria**: opens a market position on the first finished candle after start. Direction is defined by the `Direction` parameter.
- **Long/Short**: Both sides supported.
- **Exit Criteria**:
  - Take profit reached.
  - Stop loss reached.
  - Trailing stop activated and price hits the trailing level.
- **Stops**: Take profit, stop loss and trailing stop are available.
- **Default Values**:
  - `TakeProfit` = 70 price units.
  - `StopLoss` = 0 (disabled).
  - `TrailingStart` = 5 price units.
  - `TrailingSize` = 5 price units.
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
