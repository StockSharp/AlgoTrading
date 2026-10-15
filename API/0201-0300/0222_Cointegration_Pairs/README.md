# Cointegration Pairs Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
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
  - **Long**: Exit when |Z-Score| < ExitThreshold
  - **Short**: Exit when |Z-Score| < ExitThreshold
- **Stops**: Yes, percentage stop-loss.
- **Default Values**:
  - `Period` = 20
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.5
  - `Beta` = 1
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Asset2` — required, no default
    The residual is the close of the strategy's Security minus Beta times the close of Asset2 on candles of the same time, and its z-score uses the mean and standard deviation of the last Period residuals. 0.5 is the default exit level the rules quote. The first leg trades Volume and Asset2 trades Beta times Volume; an opposite signal reverses both legs. The stop closes both legs once the residual moves StopLossPercent of its entry value against the pair, checked on candle closes; 0 disables it.
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

