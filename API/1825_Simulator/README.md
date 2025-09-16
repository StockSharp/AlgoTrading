# Simulator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading EMA crossovers with optional stop loss and take profit. It buys when the fast EMA crosses above the slow EMA and sells when the fast EMA crosses below the slow EMA. Opposite signals or price targets close positions.

## Details

- **Entry Criteria**:
  - Long: fast EMA crosses above slow EMA
  - Short: fast EMA crosses below slow EMA
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite EMA crossover
  - Long: price reaches take profit or stop loss
  - Short: price reaches take profit or stop loss
- **Stops**: Fixed price offsets
- **Default Values**:
  - `FastPeriod` = 13
  - `SlowPeriod` = 50
  - `StopLoss` = 0.005m
  - `TakeProfit` = 0.005m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend-Following
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
