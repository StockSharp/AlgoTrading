# Cointegration Pairs Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy trades two assets that share a long-term cointegration relationship. By calculating the residual between the first asset and a beta-adjusted second asset, it looks for deviations that historically revert back to equilibrium.

Testing indicates an average annual return of about 103%. It performs best in the stocks market.

A long position buys the first asset and sells the second when the residual z-score drops below `-EntryThreshold`. A short position sells the first and buys the second when the z-score rises above the threshold. Positions are closed once the spread normalizes toward zero.

Cointegration pairs trading suits statistical arbitrageurs comfortable managing two instruments simultaneously. The built-in stop-loss protects against extreme moves if the relationship temporarily breaks down.

## Details
- **Entry Criteria**:
  - **Long**: Residual Z-Score < -EntryThreshold
  - **Short**: Residual Z-Score > EntryThreshold
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when |Z-Score| < 0.5
  - **Short**: Exit when |Z-Score| < 0.5
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `Period` = 20
  - `EntryThreshold` = 2.0m
  - `Beta` = 1.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Cointegration
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium

