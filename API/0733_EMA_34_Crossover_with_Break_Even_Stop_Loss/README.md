# EMA 34 Crossover with Break Even Stop Loss
[Русский](README_ru.md) | [中文](README_zh.md)

The **EMA 34 Crossover with Break Even Stop Loss** strategy enters long when price crosses above the 34-period EMA. Stop loss is placed at the previous candle's low, take profit is ten times the risk, and the stop moves to break even after price reaches three times the risk.

## Details
- **Entry Criteria**: Close crosses above EMA(34) from below.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop loss at previous low or take profit at 10× risk.
- **Stops**: Yes, break-even stop.
- **Default Values**:
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
