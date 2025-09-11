# Monthly Purchase Strategy with Dynamic Contract Size
[Русский](README_ru.md) | [中文](README_cn.md)

Buys a dynamic number of contracts on a chosen day each month using a fixed percentage of account equity. Drawdown is tracked for informational purposes.

## Details

- **Entry Criteria**: time >= StartDate AND day of month = BuyDay
- **Long/Short**: Long only
- **Exit Criteria**: none
- **Stops**: none
- **Default Values**:
  - `CandleType` = 1 day
  - `StartDate` = 2010-01-01
  - `PercentOfEquity` = 0.03
  - `BuyDay` = 1
- **Filters**:
  - Category: Dollar cost averaging
  - Direction: Long
  - Indicators: No
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Long-term
  - Seasonality: Monthly
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
