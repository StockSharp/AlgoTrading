# Relative Currency Strength
[Русский](README_ru.md) | [中文](README_cn.md)

Relative Currency Strength compares a currency pair to a basket of major currencies.
It buys when the traded pair outperforms the average of other majors and sells when it underperforms.
The comparison is based on percentage change from the start of the session.

## Details

- **Entry Criteria**: Main pair strength exceeds average by threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Strength falls below average by threshold.
- **Stops**: No.
- **Default Values**:
  - `Threshold` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Price change
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
