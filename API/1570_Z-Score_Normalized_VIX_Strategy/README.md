# Z-Score Normalized VIX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that averages z-scores of several VIX indices and enters long when combined value drops below a negative threshold.

The algorithm computes z-score for VIX, VIX3M, VIX9D and VVIX. Selected z-scores are averaged to form a single indicator representing overall volatility sentiment.

## Details

- **Entry Criteria**: Combined z-score below `-Threshold`.
- **Long/Short**: Long only.
- **Exit Criteria**: Combined z-score rising above `-Threshold`.
- **Stops**: No.
- **Default Values**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: Z-Score
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
