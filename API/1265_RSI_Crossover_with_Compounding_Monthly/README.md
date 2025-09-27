# RSI Crossover Strategy with Compounding (Monthly)
[Русский](README_ru.md) | [中文](README_zh.md)

This strategy invests the entire capital when the monthly RSI closes above its SMA and exits when RSI falls below the SMA. Gains are added to the capital for compounding.

Backtests suggest an average annual return around 20%. It works best on stocks.

## Details

- **Entry Criteria**: RSI above its SMA
- **Long/Short**: Long
- **Exit Criteria**: RSI below its SMA
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1 month
  - `RsiPeriod` = 14
  - `InitialCapital` = 100000
- **Filters**:
  - Category: Trend-following
  - Direction: Long
  - Indicators: RSI, SMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Monthly
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
