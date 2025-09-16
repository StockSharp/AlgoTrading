# Color HMA Reversal
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Hull Moving Average slope changes. It closes positions against the new direction and opens positions along the trend when the Hull MA reverses.

## Parameters
- `HmaPeriod` — period for Hull Moving Average.
- `CandleType` — type of candles to use.
- `BuyOpen`, `SellOpen` — allow opening long/short positions.
- `BuyClose`, `SellClose` — allow closing long/short positions.

## Signals
- **Upward reversal**: previous HMA was falling and current value rises → close shorts and open a long.
- **Downward reversal**: previous HMA was rising and current value falls → close longs and open a short.

The strategy uses market orders and trades with the volume specified in `Strategy.Volume`.
