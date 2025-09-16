# F2a AO Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the original MetaTrader expert advisor "F2a_AO". It filters the Awesome Oscillator with a short SMA and opens trades only in the direction of a reference candle on a higher timeframe.

The oscillator is calculated on its own timeframe. When the reference candle closes above its open, a positive filtered AO triggers a long entry and closes any shorts. When the reference candle closes below its open, a negative filtered AO triggers a short entry and closes any longs.

## Details

- **Entry Criteria**:
  - **Long**: Reference candle is bullish and filtered AO > 0.
  - **Short**: Reference candle is bearish and filtered AO < 0.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Filtered AO < 0 closes long positions.
  - Filtered AO > 0 closes short positions.
- **Stops**: No explicit stop-loss or take-profit, protection module is enabled.
- **Default Values**:
  - `IndicatorTimeFrame` = 12 hours.
  - `TrendTimeFrame` = 1 day.
  - `FastPeriod` = 13.
  - `SlowPeriod` = 144.
  - `FilterLength` = 3.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Awesome Oscillator, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
