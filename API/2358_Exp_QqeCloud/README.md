# Exp QQE Cloud Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following approach that applies the Quantitative Qualitative Estimation (QQE) indicator to a smoothed RSI.
The strategy opens positions only at a predefined session start time and closes them when the opposite signal occurs
or the trading session ends.

## Details

- **Entry Criteria**:
  - **Long**: At `StartHour`:`StartMinute`, QQE trend turns upward.
  - **Short**: At `StartHour`:`StartMinute`, QQE trend turns downward.
- **Exit Criteria**:
  - Opposite QQE trend signal.
  - Time goes beyond `StopHour`:`StopMinute`.
- **Indicators**:
  - RSI (period `RsiPeriod`, smoothed by `RsiSmoothing`).
  - QQE bands using multiplier `QqeFactor`.
- **Stops**: None by default.
- **Default Values**:
  - `CandleType` = 1-minute candles
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0, `StartMinute` = 0
  - `StopHour` = 23, `StopMinute` = 59
- **Filters**:
  - Time window for entries and exits
  - Trend-following, single timeframe
