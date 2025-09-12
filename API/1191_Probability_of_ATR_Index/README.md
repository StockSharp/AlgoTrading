# Probability of ATR Index
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Probability of ATR Index indicator.

## Details

- **Entry Criteria**: Probability crossing above or below its moving average.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `AtrDistance` = 1.5m
  - `Bars` = 8
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: ATR, SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
