# Berlin Candles Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using custom Berlin candles derived from smoothed Heikin Ashi values. A long position is opened when a bullish Berlin candle closes above the Donchian baseline. A short position is opened when a bearish Berlin candle closes below the baseline.

## Details

- **Entry Criteria**:
  - **Long**: Berlin close > Berlin open and Berlin close > baseline.
  - **Short**: Berlin close < Berlin open and Berlin close < baseline.
- **Long/Short**: Both
- **Stops**: None by default
- **Default Values**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
