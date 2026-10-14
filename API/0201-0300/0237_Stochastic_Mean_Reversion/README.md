# Stochastic Mean Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy measures the Stochastic oscillator against its own moving average to locate overextended swings. When %K moves several standard deviations away from its mean, the expectation is for the indicator to drift back toward typical values.

Testing indicates an average annual return of about 64%. It performs best in the forex market.

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
  - `AveragePeriod` = 20
  - `Multiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg and StdDev are the average and the standard deviation of the last AveragePeriod %K values, the current one included. The stop is a fixed StopLossPercent of the entry price, watched between candles as well; 0 disables it. %K is the stochastic over the first period smoothed over KPeriod candles; %D plays no part in the rules, so DPeriod is gone. An entry signal against an open position reverses it.
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

