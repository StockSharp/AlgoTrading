# Return Asymmetry Commodity
[Русский](README_ru.md) | [中文](README_zh.md)

The **Return Asymmetry Commodity** strategy exploits the difference between positive and negative returns. For each commodity future, the rolling window sums all upward and downward moves separately. A high ratio implies persistent positive drift, while a low ratio points to sustained selling pressure.

At the start of each month, commodities are ranked by this asymmetry measure. The system buys the top N contracts and sells short the weakest N, allocating capital equally. Rebalancing occurs monthly.

## Details
- **Entry Criteria**: Monthly ranking of the asymmetry of daily returns over a lookback window.
- **Long/Short**: Both directions.
- **Exit Criteria**: Positions adjusted on monthly rebalance.
- **Stops**: No explicit stop; position size capped by `MinTradeUsd`.
- **Default Values**:
  - `WindowDays = 120`
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Price based
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
