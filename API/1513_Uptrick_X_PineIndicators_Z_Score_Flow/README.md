# Uptrick X PineIndicators: Z-Score Flow Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following strategy using z-score, EMA, and RSI filters.

## Details

- **Entry Criteria**: Z-score crosses buy/sell thresholds with trend and RSI confirmation
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal based on selected mode
- **Stops**: No
- **Default Values**:
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA, RSI, StandardDeviation
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
