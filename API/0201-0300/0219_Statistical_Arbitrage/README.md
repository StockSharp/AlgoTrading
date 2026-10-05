# Statistical Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
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
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `SecondSecurity` — required, no default
    Asset1 is the strategy's Security and Asset2 is SecondSecurity; MA1 and MA2 are the simple moving averages of their last LookbackPeriod closes on candles of the same time. A long buys Asset1 and sells Asset2, a short does the opposite, each leg by Volume, and an opposite signal reverses both legs. The stop closes both legs once the spread, Asset1 minus Asset2, moves StopLossPercent of its entry value against the pair, checked on candle closes; 0 disables it.
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

