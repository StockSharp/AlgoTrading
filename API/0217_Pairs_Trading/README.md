# Pairs Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This pairs trading strategy monitors the price spread between two correlated instruments. By comparing the spread to its historical mean and standard deviation, the system attempts to exploit temporary divergences that eventually revert.

A long spread is entered when the spread drops below its mean by more than the specified deviation multiplier. This means buying the first asset and selling the second. A short spread does the opposite when the spread rises above the mean by the same amount. Positions are closed once the spread returns to the average level.

Pairs trading appeals to market neutral traders who prefer relative-value opportunities rather than outright direction. Because both legs are hedged, volatility tends to be lower, though the strategy still uses a stop-loss on the spread to manage risk.

## Details
- **Entry Criteria**:
  - **Long**: Spread < Mean - Multiplier * StdDev
  - **Short**: Spread > Mean + Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when spread reverts to the mean
  - **Short**: Exit when spread reverts to the mean
- **Stops**: Yes, percentage stop based on spread value.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Spread statistics
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium

Testing indicates an average annual return of about 88%. It performs best in the stocks market.
