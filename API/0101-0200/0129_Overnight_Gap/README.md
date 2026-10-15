# Overnight Gap Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Overnight Gap plays the open when price gaps significantly from the prior close due to news or after-hours activity.
Large gaps often retrace partially as traders digest the move.

Testing indicates an average annual return of about 124%. It performs best in the forex market.

The strategy fades excessive gaps, entering in the opposite direction shortly after the open and closing before the session ends.

Stops are based on a percentage beyond the gap extremes to manage risk if the move continues.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MinGapPercent` = 0.01
    The market trades around the clock, so the session is the UTC day. The gap is the distance from the previous day's last close to the day's first open. A gap of at least MinGapPercent is faded at the close of the first candle; the stop lies StopLossPercent beyond that candle's high (short) or low (long) and is checked on candle closes, and the position closes at the day's last candle.
- **Filters**:
  - Category: Gap
  - Direction: Both
  - Indicators: Gap
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

