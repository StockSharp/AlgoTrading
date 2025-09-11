# McClellan A-D Volume Integration Model Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy builds a weighted advance-decline line by multiplying the price spread of the bar by its volume. Two EMAs of this weighted line form a McClellan-style oscillator.

A long position is opened when the oscillator crosses above a user-defined threshold after being below it. The trade is closed automatically after a fixed number of bars.

## Details

- **Entry**: oscillator crosses above `Long Entry Threshold` from below.
- **Exit**: position closed after `Exit After Bars` candles.
- **Long/Short**: long only.
- **Indicators**: two EMAs.
- **Stops**: none.
- **Timeframe**: configurable.
