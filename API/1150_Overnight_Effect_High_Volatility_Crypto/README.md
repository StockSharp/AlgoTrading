# Overnight Effect High Volatility Crypto Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that enters a long position during high-volatility evenings and closes before midnight. Volatility is measured by the standard deviation of log returns over a configurable period and compared against the median of historical volatility.

## Details

- **Entry Criteria**:
  - `currentHour == EntryHour && highVolatility` when `UseVolatilityFilter`
  - `currentHour == EntryHour` when filter disabled
- **Long/Short**: Long
- **Stops**: None
- **Default Values**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Time-based
  - Direction: Long
  - Indicators: StandardDeviation, Median
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
