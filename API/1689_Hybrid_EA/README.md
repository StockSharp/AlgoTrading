# Hybrid EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Hybrid EA strategy uses the Relative Vigor Index (RVI) and its signal line.
It opens a long position when the RVI rises above the signal by a specified difference and opens a short position when it falls below by the same amount. Positions are protected by fixed take profit and stop loss levels measured in price points.

## Details

- **Entry Criteria**: RVI minus signal exceeding threshold
- **Long/Short**: Both
- **Exit Criteria**: opposite threshold cross or take-profit/stop-loss
- **Stops**: Yes, fixed distance in points
- **Default Values**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 minute candles
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RVI, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
