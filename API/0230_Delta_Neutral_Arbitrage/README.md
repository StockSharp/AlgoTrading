# Delta Neutral Arbitrage Strategy

This arbitrage strategy trades the spread between two correlated assets while keeping the combined position close to delta neutral. By balancing a long position in one asset against a short in another, it attempts to profit from mean reversion in the spread rather than market direction.

A long spread is entered when the z-score of the price difference falls below `-EntryThreshold`. The first asset is bought and the second is sold in equal size. A short spread does the reverse when the z-score rises above the positive threshold. The trade is closed once the spread returns to the moving average.

Delta neutral trading is popular among quantitative traders seeking low volatility exposure. Although hedged, stop-loss protection is still applied to guard against extreme divergence between the assets.

## Details
- **Entry Criteria**:
  - **Long**: Spread Z-Score < -EntryThreshold
  - **Short**: Spread Z-Score > EntryThreshold
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when spread crosses back above mean
  - **Short**: Exit when spread crosses back below mean
- **Stops**: Yes, percent stop-loss on spread value.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2m
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
