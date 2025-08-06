# Consistent Momentum Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

The **Consistent Momentum** strategy selects instruments that exhibit strong momentum across two time windows and rebalances the portfolio monthly. It holds each tranche for a fixed number of months and allocates capital equally to long and short baskets.

## Details
- **Entry Criteria**: On the first trading day of each month, go long on securities in the top decile of both momentum measures and short the bottom decile.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions are closed after the holding period expires or when rebalancing occurs.
- **Stops**: No explicit stop logic; position size is based on dollar allocation.
- **Default Values**:
  - `LookbackDays = 7 * 21`
  - `HoldingMonths = 6`
  - `MinTradeUsd = 50`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Price momentum
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
