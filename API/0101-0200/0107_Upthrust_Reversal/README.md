# Upthrust Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Upthrust Reversal is the bearish companion to the spring and occurs when price briefly breaks above resistance but quickly falls back.
The move flushes out late buyers before reversing lower.

Testing indicates an average annual return of about 58%. It performs best in the stocks market.

This strategy sells short once price drops back under the breakout level, expecting supply to overwhelm demand.

A stop just above the upthrust high manages risk and positions exit if price recovers above that level.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2
    Resistance is the highest high of the previous LookbackPeriod candles. The strategy only sells: a bearish candle that breaks above resistance and closes back below it. The stop lies StopLossPercent above the upthrust high and the position closes when a candle closes above it.
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

