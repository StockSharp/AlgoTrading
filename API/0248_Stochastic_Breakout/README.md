# Stochastic Breakout Strategy

This breakout approach monitors the Stochastic oscillator for sharp moves away from its recent average. When the %K line breaks above or below a volatility-adjusted threshold, it signals a burst of momentum that may start a trend.

A long position is triggered when %K crosses above the upper threshold after a period of contraction. A short position is taken when %K breaks below the lower threshold. The trade is closed when the oscillator drifts back toward its average or hits a protective stop.

The strategy is designed for intraday traders who want early entry into momentum swings. Using volatility-based bands helps filter noise so only decisive moves create signals.

## Details
- **Entry Criteria**:
  - **Long**: %K > Avg + DeviationMultiplier * StdDev
  - **Short**: %K < Avg - DeviationMultiplier * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when %K < Avg
  - **Short**: Exit when %K > Avg
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `StochasticPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
