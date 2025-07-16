# MACD Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This method tracks the MACD histogram relative to its own average. Extreme histogram readings often revert once momentum subsides. By monitoring the difference between MACD and its signal line, the strategy finds overextended moves.

A long position is entered when the MACD histogram falls below the mean by `DeviationMultiplier` standard deviations. A short position is opened when the histogram rises above the mean by the same amount. The trade is closed when the histogram crosses back through its average.

This approach caters to traders comfortable fading momentum extremes. A stop-loss measured as a percentage of entry price guards against trends that continue to strengthen.

## Details
- **Entry Criteria**:
  - **Long**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **Short**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Histogram > Avg
  - **Short**: Exit when Histogram < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
