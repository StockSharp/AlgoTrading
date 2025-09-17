# Multi Time-Frame Trader
[Русский](README_ru.md) | [中文](README_cn.md)

A multi-timeframe strategy that combines linear regression channels on M1, M5, and H1 candles. The regression slope from the H1 channel defines the dominant trend, while the M5 and M1 channels provide precise entry locations near support and resistance.

## Trading logic

- **Data feeds**: nine timeframes of standard candles (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
- **Indicators**: each feed is processed by a linear regression channel of configurable length. The channel provides a center line and symmetric upper/lower bands based on the maximum deviation of recent closes.
- **Trend filter**: the strategy only considers short trades when the H1 channel slope is negative and long trades when it is positive.
- **Entry**:
  - **Short** – the latest M5 high and M1 high both pierce their upper channel bands while the H1 slope is negative.
  - **Long** – the latest M5 low and M1 low both reach their lower channel bands while the H1 slope is positive.
- **Order handling**: entries are executed with market orders using the configured volume. Stop-loss and take-profit targets are derived from the M5 channel half-width and center line respectively.
- **Exit**: positions are closed on the M1 candles when the price hits the protective stop or the center line target.
- **Position management**: at most one market position is open at any time.

## Parameters

| Name | Description |
| --- | --- |
| `EnableTrading` | Allows the strategy to place orders when enabled. |
| `BarsToCount` | Number of bars used in every regression channel (default 50). |
| `Volume` | Market order volume in lots. |

## Notes

- Longer regression windows provide smoother channel slopes but slower reactions.
- The multi-timeframe slope display is useful for monitoring alignment across higher intervals even though only the H1 slope gates entries.
- Protective levels are recalculated each time a new M5 candle forms; frequent recalibration keeps risk tightly coupled to the current channel geometry.
