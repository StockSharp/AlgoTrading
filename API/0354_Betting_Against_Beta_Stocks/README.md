# Betting Against Beta Stocks
[Русский](README_ru.md) | [中文](README_zh.md)

The **Betting Against Beta Stocks** strategy longs the lowest beta decile of a stock universe and shorts the highest beta decile. Rebalancing occurs on the first trading day of each month.

The approach aims to exploit the anomaly that low-beta stocks tend to outperform on a risk-adjusted basis. It assumes access to a benchmark security for beta calculations.

## Details
- **Entry Criteria**: Monthly selection of low/high beta stocks.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions are adjusted at the next rebalance.
- **Stops**: No explicit stop logic.
- **Default Values**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filters**:
  - Category: Statistical
  - Direction: Both
  - Indicators: Beta
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
