# Supertrend And MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Supertrend, MACD, and EMA 200 filter.

## Details

- **Entry Criteria**: Price above or below Supertrend and EMA, MACD line vs signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: MACD crossover or stop based on recent extremes.
- **Stops**: Highest/Lowest trailing stops.
- **Default Values**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SuperTrend, EMA, MACD, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
