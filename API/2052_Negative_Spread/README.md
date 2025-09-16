# Negative Spread Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Negative Spread strategy exploits rare moments when the best ask price falls below the best bid price, creating a negative spread.
When this mispricing appears, the strategy sells at market and attempts to capture the abnormal spread.
After the short position is opened, it is closed on the next order book update once the market returns to a normal state.

The system listens only to order book events and does not rely on candles or indicators.
Optional stop-loss and take-profit parameters are provided as safety measures and are calculated in pips using the instrument's tick size.

## Details
- **Entry Criteria**: `BestAsk < BestBid` and no active position.
- **Long/Short**: Short only.
- **Exit Criteria**: Position is closed immediately after it opens.
- **Stops**: Optional stop-loss and take-profit in pips.
- **Default Values**:
  - `Volume` = 1
  - `TakeProfitPips` = 5000
  - `StopLossPips` = 5000
- **Filters**:
  - Category: Arbitrage
  - Direction: Short
  - Indicators: None
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Tick
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
