# NRTR Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows market trends using the **NRTR (Nick R's Trend Reverse)** indicator. The algorithm calculates a trailing stop level derived from the average range of recent candles. When price breaks the trailing level, the position reverses in the direction of the breakout. The system works on both long and short sides and includes optional stop-loss and take-profit protections.

The NRTR length defines the sensitivity of the trailing stop: a shorter period reacts faster but may whipsaw, while a longer period filters noise. An additional digit shift parameter adjusts the indicator to instruments with different price scales. The strategy subscribes to candles of the chosen timeframe and computes the NRTR values on each finished bar.

## Details

- **Entry Logic**:
  - **Long**: Price crosses above the NRTR level after a downtrend.
  - **Short**: Price crosses below the NRTR level after an uptrend.
- **Exit Logic**:
  - Positions are reversed when an opposite breakout occurs.
- **Stops**: Optional stop-loss and take-profit via `StartProtection`.
- **Default Values**:
  - `Length` = 10
  - `DigitsShift` = 0
  - `TakeProfit` = 2000 points
  - `StopLoss` = 1000 points
  - `CandleType` = 1-hour candles
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: NRTR, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Flexible
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
