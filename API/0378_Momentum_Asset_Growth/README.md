# Momentum Asset Growth Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This hybrid factor strategy marries price momentum with the asset-growth effect.
Firms that rapidly expand their balance sheets and simultaneously show strong
trending prices are often rewarded by the market. The approach first filters the
universe for companies in the highest decile of asset growth.

Eligible stocks are then ranked on twelve-month momentum, excluding the most
recent month to avoid short-term reversals. The top momentum quintile is bought
while the bottom quintile is sold short. Rebalancing takes place on the first
trading day of each month except January when the strategy stays idle. No
stop-losses are applied between reviews.

Backtests across developed equities indicate the blend of asset expansion and
momentum delivers robust returns with moderate turnover.

## Details

- **Entry Criteria**: Monthly; select top asset-growth decile then rank by
  momentum; long top quintile, short bottom quintile
- **Long/Short**: Both
- **Exit Criteria**: Next monthly rebalance (January skipped)
- **Stops**: No
- **Default Values**:
  - `MomLook` = 252
  - `SkipMonths` = 1
  - `AssetDecile` = 10
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Momentum, Fundamentals
  - Direction: Both
  - Indicators: Price momentum, asset growth
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Medium-term
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
