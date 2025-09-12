# Spearman Rank Correlation Coefficient Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This pair trading strategy measures the Spearman rank correlation between two securities. When the correlation exceeds a positive threshold the strategy goes short the first security and long the second. When it drops below the negative threshold it takes the opposite position. Positions are closed when the correlation returns toward zero.

## Details

- **Entry Criteria:**
  - **Long First / Short Second**: correlation < -Threshold.
  - **Short First / Long Second**: correlation > Threshold.
- **Long/Short**: Pair trading.
- **Exit Criteria:**
  - Correlation absolute value < Threshold / 2.
- **Stops**: No.
- **Default Values:**
  - `CorrelationPeriod` = 10
  - `Threshold` = 0.8
- **Filters:**
  - Category: Correlation
  - Direction: Both
  - Indicators: Spearman Rank Correlation
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
