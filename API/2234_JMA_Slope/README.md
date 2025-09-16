# JMA Slope Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors the slope of the Jurik Moving Average (JMA). A position is opened when the slope crosses zero or when its direction changes depending on the selected mode.

## Details

- **Entry Criteria**:
  - **Long**: Slope crosses below zero or turns upward (mode dependent).
  - **Short**: Slope crosses above zero or turns downward.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Opposite signal reverses the position.
- **Stops**: None.
- **Default Values**:
  - `JMA Length` = 14
  - `JMA Phase` = 0
  - `Mode` = Breakdown
  - `Candle Type` = 4h timeframe
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: JMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: 4h
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
