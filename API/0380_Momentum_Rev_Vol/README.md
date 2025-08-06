# Momentum Rev Vol Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This composite factor strategy blends three signals: long-term momentum,
short-term reversal and low volatility. Each month a score is calculated for
every security using 12‑month momentum, the inverse of one‑month returns and the
trailing 60‑day volatility. Adjustable weights `WM`, `WR` and `WV` control the
contribution of each component.

On the first trading day of every month securities are ranked by the composite
score. The highest decile is bought and the lowest decile is shorted with equal
dollar weights. Positions are held until the next rebalance and no explicit
stop-loss rules are employed.

By combining trend following, mean reversion and risk aversion, the strategy
seeks diversified returns across different market regimes.

## Details

- **Entry Criteria**: Monthly ranking by weighted combination of momentum,
  reversal and volatility; long top decile, short bottom decile
- **Long/Short**: Both
- **Exit Criteria**: Next monthly rebalance
- **Stops**: No
- **Default Values**:
  - `Lookback12` = 252
  - `Lookback1` = 21
  - `VolWindow` = 60
  - `WM` = 1.0
  - `WR` = 1.0
  - `WV` = 1.0
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Multi-factor
  - Direction: Both
  - Indicators: Momentum, reversal, volatility
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
