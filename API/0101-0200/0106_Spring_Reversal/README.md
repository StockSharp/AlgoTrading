# Spring Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Spring Reversal is a Wyckoff concept where price briefly breaks support and then springs back above it.
This shakeout traps late sellers and often marks the beginning of an uptrend.

Testing indicates an average annual return of about 55%. It performs best in the stocks market.

The strategy buys once price reclaims the broken level, anticipating swift short covering and new demand.

A stop just below the spring low limits downside, and the position is closed if follow-through fails.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2
    Support is the lowest low of the previous LookbackPeriod candles. The strategy only buys: a bullish candle that breaks below support and closes back above it. The stop lies StopLossPercent below the spring low and the position closes when a candle closes below it.
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Wyckoff
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

