# Accrual Anomaly
[Русский](README_ru.md) | [中文](README_zh.md)

The **Accrual Anomaly** strategy implements the accrual anomaly factor. It rebalances annually on the first trading day of May, going long low-accrual stocks and short high-accrual ones.

Testing indicates an average annual return of about 12%. It performs best in the U.S. equity market.

Positions are adjusted once per year; no intraday signals are used.

## Details
- **Entry Criteria**: see implementation for accrual calculations.
- **Long/Short**: Both directions.
- **Exit Criteria**: Rebalance on next scheduled date.
- **Stops**: No explicit stop logic.
- **Default Values**:
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Fundamentals
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
