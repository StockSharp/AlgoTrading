# Color XXDPO Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using a double smoothed Detrended Price Oscillator to capture slope reversals.

## Details
- **Entry Criteria**: Upward slope with current value rising opens long; downward slope with current value falling opens short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite slope change closes positions.
- **Stops**: None.
- **Default Values**: First MA Length 21, Second MA Length 5, Candle Timeframe 6 hours.
- **Filters**: None.
