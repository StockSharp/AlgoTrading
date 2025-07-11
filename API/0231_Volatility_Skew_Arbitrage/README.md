# Volatility Skew Arbitrage Strategy

This options-based strategy observes the difference in implied volatility between two strikes. When the skew diverges from its historical average by a large margin, it opens a position expecting the skew to revert.

A long skew trade buys the cheaper-volatility option and sells the expensive one when the skew exceeds `Threshold` standard deviations above the mean. A short skew trade does the opposite when the skew falls below the mean by the same amount. Positions are closed when the skew moves back toward its average level.

The strategy is designed for experienced traders familiar with options pricing. Stop-loss protection is used to guard against persistent shifts in volatility expectations.

## Details
- **Entry Criteria**:
  - **Long**: Volatility skew > average + Threshold * StdDev
  - **Short**: Volatility skew < average - Threshold * StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when skew returns toward average
  - **Short**: Exit when skew returns toward average
- **Stops**: Yes, percent stop-loss on option positions.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `Threshold` = 2m
  - `StopLossPercent` = 2m
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Volatility Skew
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: High
