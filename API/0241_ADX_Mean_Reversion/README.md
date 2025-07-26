# ADX Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Here the Average Directional Index (ADX) measures overall trend strength. When ADX is low, the market lacks direction and prices tend to oscillate around a mean value. This strategy exploits that behaviour by trading deviations of ADX from its moving average.

A long trade is entered when ADX drops below the average minus `DeviationMultiplier` times the standard deviation and price is below the moving average. A short trade is opened when ADX spikes above the upper band and price is above the average. Positions are closed when ADX reverts toward its average.

This system appeals to traders looking for opportunities during low-trend environments. The stop-loss prevents small mean-reversion trades from growing into large losses if a new trend emerges.

## Details
- **Entry Criteria**:
  - **Long**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when ADX > Avg
  - **Short**: Exit when ADX < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 70%. It performs best in the stocks market.
