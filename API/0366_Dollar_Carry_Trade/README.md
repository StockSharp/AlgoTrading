# Dollar Carry Trade
[Русский](README_ru.md) | [中文](README_zh.md)

The **Dollar Carry Trade** strategy ranks USD currency pairs by interest-rate differential and goes long USD against low-carry currencies and short against high-carry currencies. Rebalances monthly on the first trading day.

## Details
- **Entry Criteria**: Rank by carry; long low-carry, short high-carry.
- **Long/Short**: Both.
- **Exit Criteria**: Monthly rebalance.
- **Stops**: No explicit stop.
- **Default Values**:
  - `K = 3`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Rates
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
