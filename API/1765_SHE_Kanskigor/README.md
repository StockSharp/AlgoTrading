# SHE Kanskigor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This daily strategy opens a single position each day based on the direction of the previous day's candle. At the configured time it buys if the prior day closed below its open and sells if it closed above. A fixed take-profit and stop-loss measured in price steps manage risk. Only one trade is allowed per day.

## Details

- **Entry Criteria**: At `StartTime` compare the previous day's open and close; buy when `open > close`, sell when `open < close`
- **Long/Short**: Both
- **Exit Criteria**: take profit or stop loss
- **Stops**: Yes
- **Default Values**:
  - `Volume` = 0.1
  - `StartTime` = 00:05
  - `TakeProfit` = 350
  - `StopLoss` = 550
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
