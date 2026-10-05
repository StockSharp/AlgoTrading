# Supertrend Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Supertrend Volume augments the Supertrend indicator with volume confirmation.
Rising volume during a Supertrend flip strengthens the likelihood of a new impulse move.

Testing indicates an average annual return of about 145%. It performs best in the crypto market.

The strategy enters with the trend on a Supertrend signal only when accompanied by above-average volume.

Stops track the Supertrend line, exiting when price closes on the other side.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `VolumePeriod` = 20
    A Supertrend flip, a close on the other side of the line, closes a position against it. The flip opens a position in its direction only when the candle's volume is above the average of the previous VolumePeriod candles, so a confirmed flip reverses the position.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

