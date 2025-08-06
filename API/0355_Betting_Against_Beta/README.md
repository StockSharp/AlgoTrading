# Betting Against Beta
[Русский](README_ru.md) | [中文](README_zh.md)

The **Betting Against Beta** strategy goes long on the lowest-beta assets and short on the highest-beta ones. Betas are
calculated against a benchmark over a rolling window and the portfolio is rebalanced on the first trading day of each
month.

## Details
- **Entry Criteria**: rank universe by beta relative to the benchmark; long lowest decile, short highest decile.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions adjusted at the next monthly rebalance.
- **Stops**: No explicit stop logic.
- **Default Values**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filters**:
  - Category: Factor
  - Direction: Both
  - Indicators: Statistical
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
