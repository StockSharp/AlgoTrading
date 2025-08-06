# ROA Effect Stocks
[Русский](README_ru.md) | [中文](README_zh.md)

The **ROA Effect Stocks** strategy targets equities with high returns on assets. An external fundamental data feed supplies the ROA values for the trading universe. At the beginning of each month, the stocks are ranked by ROA, and the portfolio goes long the top decile and short the bottom decile.

Positions are sized equally and rebalanced monthly, capturing the tendency for profitable firms to outperform.

## Details
- **Entry Criteria**: Monthly ranking by external ROA data.
- **Long/Short**: Both directions.
- **Exit Criteria**: Monthly rebalance.
- **Stops**: No explicit stop.
- **Default Values**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
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
