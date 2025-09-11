# LUBE Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy measures "friction" around the current close price by scanning previous candles. A FIR filter defines trend direction. 

- **Long** when friction drops below the trigger level and trend is up.
- **Short** when friction drops below the trigger level and trend is down.
- **Exit** when friction rises above mid level or opposite signal appears.

## Details
- **Indicators**: custom friction calculation, FIR filter.
- **Timeframe**: 30m candles by default.
- **Both sides**: yes, shorts optional.
