# Best Dollar Cost Average Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy accumulates a position by investing a fixed amount of capital at regular
intervals between user-defined start and end dates. Each purchase occurs at the close
of the selected timeframe regardless of price, implementing a classic dollar cost
averaging approach.

## Details

- **Entry Criteria**:
  - At each interval (daily, weekly, or monthly) between the start and end dates the
    strategy buys using the closing price for the configured amount.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Positions are held; no automatic exit logic is included.
- **Stops**: None.
- **Default Values**:
  - Amount per period = 100.
  - Interval = Weekly.
  - Start date = 2018-01-01, End date = 2020-01-28.
- **Filters**:
  - Category: Accumulation.
  - Direction: Long.
  - Indicators: None.
  - Stops: No.
  - Complexity: Low.
  - Timeframe: Any.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Low.
