# Stochastic Mean Reversion Strategy

This strategy measures the Stochastic oscillator against its own moving average to locate overextended swings. When %K moves several standard deviations away from its mean, the expectation is for the indicator to drift back toward typical values.

A long trade is placed when Stochastic %K falls below the lower band defined by the average minus `Multiplier` times the standard deviation. A short trade occurs when %K exceeds the upper band. Positions are closed once %K crosses back through its average line.

The method is designed for short-term traders who like to trade overbought and oversold extremes. The stop-loss protects against sustained momentum that fails to mean revert.

## Details
- **Entry Criteria**:
  - **Long**: %K < Avg - Multiplier * StdDev
  - **Short**: %K > Avg + Multiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when %K > Avg
  - **Short**: Exit when %K < Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
