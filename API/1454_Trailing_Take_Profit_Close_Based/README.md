# Trailing Take Profit - Close Based
[Русский](README_ru.md) | [中文](README_cn.md)

This long-only strategy enters on a fast/slow SMA crossover. A fixed profit target is set from the entry price. When the target is reached the stop can trail below the highest close to lock in gains.

## Details

- **Entry**: Fast SMA crosses above slow SMA.
- **Exit**: Reverse crossover or take profit.
- **Indicators**: SMA.
- **Timeframe**: Any.
- **Risk**: Optional trailing stop.
