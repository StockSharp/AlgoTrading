# Momentum Factor Stocks Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This systematic approach harnesses the classic 12‑1 month momentum factor in
equities. At the end of each month stocks are ranked by their performance over
the prior twelve months while skipping the most recent month to sidestep
short-term reversals. Securities in the highest quintile are purchased and those
in the lowest quintile are sold short, forming a market-neutral spread.

Rebalancing occurs on the first trading day of every month. Positions are
equally weighted and remain open until the next rebalance; no explicit
stop-losses are used.

Extensive academic and industry research shows momentum delivers persistent
excess returns and offers valuable diversification when combined with other
factors.

## Details

- **Entry Criteria**: Monthly 12‑1 momentum ranking; long top quintile, short
  bottom quintile
- **Long/Short**: Both
- **Exit Criteria**: Next monthly rebalance
- **Stops**: No
- **Default Values**:
  - `LookbackDays` = 252
  - `SkipDays` = 21
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Price change
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
