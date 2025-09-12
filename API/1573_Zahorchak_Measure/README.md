# Zahorchak Measure Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Calculates a weighted score using multiple moving averages. Buys when the score turns positive and sells when it turns negative.

## Details

- **Entry Criteria**: Score crosses above zero
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Points` = 1
  - `EmaLength` = 10
- **Filters**:
  - Category: Breadth
  - Direction: Both
  - Indicators: SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
