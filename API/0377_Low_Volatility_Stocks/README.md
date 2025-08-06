# Low Volatility Stocks Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This defensive equity factor seeks out the "low volatility anomaly"—the
observation that stocks with calmer price movements often deliver superior
risk-adjusted returns. Volatility is calculated as the standard deviation of
daily returns over a trailing window (60 trading days by default).

On the first trading day of each month the universe is ranked by realized
volatility. The strategy goes long the lowest-volatility decile and shorts the
highest-volatility decile, allocating equal dollar weights within each bucket.
Positions are held until the next monthly rebalance and no explicit stop-losses
are used.

Backtests show a smoother equity curve and smaller drawdowns than the broad
market, making the approach attractive for investors seeking equity exposure
with reduced risk.

## Details

- **Entry Criteria**: Monthly sort by trailing volatility; long lowest decile,
  short highest decile
- **Long/Short**: Both
- **Exit Criteria**: Next monthly rebalance
- **Stops**: No
- **Default Values**:
  - `VolWindowDays` = 60
  - `Deciles` = 10
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: Standard deviation
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
