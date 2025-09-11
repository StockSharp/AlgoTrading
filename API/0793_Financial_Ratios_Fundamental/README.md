# Financial Ratios Fundamental Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy analyzes quarterly financial ratios to gauge a company's fundamentals. It looks at the current ratio, interest coverage, payable turnover and gross margin, entering long positions when any of these ratios improve compared to the previous period.

## Details

- **Entry Criteria**:
  - **Long**: `currentRatio > previousCurrent` OR `interestCoverage < previousInterest` OR `payableTurnover > previousPayable` OR `grossMargin > previousGross`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: `currentRatio < previousCurrent` OR `interestCoverage > previousInterest` OR `payableTurnover < previousPayable` OR `grossMargin < previousGross`.
- **Stops**: No.
- **Default Values**:
  - `Candle Type` = daily candles.
- **Filters**:
  - Category: Fundamental
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Long-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
