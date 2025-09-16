# Coppock Histogram Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades reversals of the Coppock Histogram. The indicator sums two Rate of Change values and smooths the result with a moving average. When momentum turns upward the strategy opens long positions and closes shorts. A downward turn closes longs and enters shorts. Signals are evaluated on completed candles only.

## Details

- **Entry Criteria**: Coppock histogram slopes up for buys or down for sells.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal closes open positions.
- **Stops**: No explicit stop loss or take profit.
- **Default Values**:
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RateOfChange, SimpleMovingAverage
  - Stops: None
  - Complexity: Basic
  - Timeframe: 8H
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
