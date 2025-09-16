# Fracture
[Русский](README_ru.md) | [中文](README_cn.md)

Fracture combines fractal breakouts with smoothed moving averages and ADX to trade both ranging and trending markets.

## Details

- **Entry Criteria**: If ADX is below the threshold, go long above the last up fractal or short below the last down fractal when price is also above/below the fast SMMA. In a trending regime (fast SMMA above/below slower ones), enter in the trend direction on price crossing the fast SMMA.
- **Long/Short**: Both.
- **Exit Criteria**: Close position once profit exceeds ATR multiplied by `MinProfit`.
- **Stops**: ATR-based profit target.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Long & Short
  - Indicators: Fractal, SMMA, ATR, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
