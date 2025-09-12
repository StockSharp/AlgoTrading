# Most Powerful TQQQ EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters long when a fast EMA crosses above a slow EMA. A take-profit and stop-loss are set as multipliers of the entry price.

## Details

- **Entry Criteria**: Fast EMA crossing above slow EMA
- **Long/Short**: Long only
- **Exit Criteria**: Price hitting take-profit or stop-loss level
- **Stops**: Yes (fixed multiplier)
- **Default Values**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
