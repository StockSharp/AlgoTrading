# EMA 2-35 Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows a simple crossover between two Exponential Moving Averages. The fast EMA with length 2 reacts quickly to price changes, while the slow EMA with length 35 represents the longer-term trend. A position is opened when the fast EMA crosses the slow EMA; positions are reversed when the opposite crossover occurs.

Risk management is handled with fixed stop-loss and take-profit levels expressed in price steps. A trailing stop is also applied to lock in profits as the trade moves in a favorable direction.

## Details

- **Entry Criteria**:
  - **Long**: EMA(2) crosses above EMA(35).
  - **Short**: EMA(2) crosses below EMA(35).
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite crossover.
  - Stop-loss or take-profit hit.
  - Trailing stop triggered.
- **Stops**: Fixed stop-loss, take-profit, and trailing stop (all in price steps).
- **Default Values**:
  - `FastLength` = 2
  - `SlowLength` = 35
  - `StopLoss` = 50
  - `TakeProfit` = 150
  - `TrailingStop` = 50
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving averages
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Short-term

