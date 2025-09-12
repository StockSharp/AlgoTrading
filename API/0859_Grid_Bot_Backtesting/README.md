# Grid Bot Backtesting Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements a grid trading bot that accumulates long positions when price falls to grid levels and closes them when price moves to the next line. Bounds may be set manually or calculated from recent data.

## Details

- **Entry Criteria**:
  - **Long**: price crosses below a grid line with no active order
- **Long/Short**: Long only
- **Exit Criteria**:
  - price crosses above the next grid line
- **Stops**: None
- **Default Values**:
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **Filters**:
  - Category: Range trading
  - Direction: Long
  - Indicators: Highest, Lowest, SimpleMovingAverage
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
