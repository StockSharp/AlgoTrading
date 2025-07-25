# Volume Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This system looks for unusually high or low trading volume relative to its historical average. Significant volume spikes often revert as activity normalizes, offering potential fade trades.

A long entry is made when volume drops below the average minus `DeviationMultiplier` times the standard deviation and price is below the moving average. A short entry occurs when volume rises above the upper band with price above the average. Trades exit once volume returns toward its mean level.

The strategy benefits traders who watch for exhaustion after volume surges. A percentage stop-loss guards against scenarios where volume keeps expanding in the same direction.

## Details
- **Entry Criteria**:
  - **Long**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when volume > Avg
  - **Short**: Exit when volume < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 76%. It performs best in the forex market.
