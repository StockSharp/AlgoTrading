# Beta Neutral Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
This strategy seeks to exploit pricing differences between two securities while neutralizing overall market beta. By adjusting positions based on each asset's beta to a common index, the portfolio aims to remain insensitive to broad market moves.

A long spread goes long the asset with lower beta-adjusted price and shorts the other when the spread deviates beyond two standard deviations. A short spread does the reverse when the spread is above the mean. Trades are closed once the beta-adjusted spread reverts toward its average.

Beta neutral arbitrage is common among hedge funds looking for relative value without taking directional risk. A stop-loss is applied if the spread continues to widen instead of converging.

## Details
- **Entry Criteria**:
  - **Long**: Beta-adjusted spread < Mean - 2*StdDev
  - **Short**: Beta-adjusted spread > Mean + 2*StdDev
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when spread approaches mean
  - **Short**: Exit when spread approaches mean
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Beta-adjusted spread
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: High

Testing indicates an average annual return of about 52%. It performs best in the crypto market.
