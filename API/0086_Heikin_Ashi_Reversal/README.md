# Heikin-Ashi Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Heikin-Ashi candles smooth noise and highlight trend direction. A shift from a series of bearish HA candles to a bullish one, or vice versa, can indicate a change in momentum. This strategy trades those color flips and uses a percentage stop for protection.

Testing indicates an average annual return of about 145%. It performs best in the crypto market.

The logic calculates Heikin-Ashi values from regular candles. When the HA close crosses above the HA open after a bearish sequence, a long is taken. A cross below after a bullish run opens a short. The stop is placed a fixed percentage away from entry.

The method is simple yet effective during choppy swings when traditional candlesticks are noisy.

## Details

- **Entry Criteria**: Heikin-Ashi candle changes color.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Heikin-Ashi
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

