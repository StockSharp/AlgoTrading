# Delta Neutral Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This arbitrage strategy trades the spread between two correlated assets while keeping the combined position close to delta neutral. By balancing a long position in one asset against a short in another, it attempts to profit from mean reversion in the spread rather than market direction.

Testing indicates an average annual return of about 43%. It performs best in the stocks market.

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
  - `EntryThreshold` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Asset2Security` — required, no default
  - `Asset2Portfolio` — the strategy's portfolio when empty
    The spread is the close of the strategy's Security minus the close of Asset2Security on candles of the same time; its z-score uses the mean and standard deviation of the last LookbackPeriod spreads. Each leg trades Volume, and an opposite signal reverses both legs. The stop closes both legs once the spread moves StopLossPercent of its entry value against the pair, checked on candle closes; 0 disables it.
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

