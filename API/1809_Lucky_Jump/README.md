# Lucky Jump Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Lucky Jump strategy is a short-term mean reversion system that reacts to sudden price jumps on the best bid and ask. When the ask price jumps upward by a specified number of points compared to the previous quote, the strategy opens a short position expecting a pullback. Conversely, when the bid price drops by the same amount, it enters long. Positions are closed either at the first favorable tick or when the loss exceeds a predefined limit.

This approach attempts to capture quick corrections after aggressive market moves. It operates purely on Level1 quote data and does not rely on candles or indicators.

## Details

- **Entry Criteria**:
  - **Short**: `Ask(t) - Ask(t-1) >= Shift * PriceStep`.
  - **Long**: `Bid(t-1) - Bid(t) >= Shift * PriceStep`.
- **Exit Criteria**:
  - Close position as soon as it becomes profitable.
  - Close if loss exceeds `Limit * PriceStep`.
- **Stops**: implicit stop based on `Limit` parameter.
- **Default Values**:
  - `Shift` = 30 points.
  - `Limit` = 180 points.
  - `Volume` = 1.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Simple
  - Timeframe: Ultra short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High

