# Statistical Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This statistical arbitrage approach trades a pair of related securities based on their relative positioning around moving averages. By comparing each asset to its own average, the strategy seeks to exploit short-term dislocations that should converge over time.

Testing indicates an average annual return of about 94%. It performs best in the stocks market.

A long position is initiated when the first asset trades below its moving average while the second asset trades above its own average. A short position occurs when the first asset is above its average and the second is below. Positions are closed when the first asset crosses back through its moving average, signalling the spread has normalized.

The method is ideal for market-neutral traders comfortable balancing exposure across two instruments. The built-in stop-loss limits drawdowns if the spread widens further instead of reverting.

## Details
- **Entry Criteria**:
  - **Long**: Asset1 < MA1 && Asset2 > MA2
  - **Short**: Asset1 > MA1 && Asset2 < MA2
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Asset1 closes above its MA1
  - **Short**: Exit when Asset1 closes below its MA1
- **Stops**: Yes, percent stop-loss on spread.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Arbitrage
  - Direction: Both
  - Indicators: Moving Averages
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk Level: Medium

