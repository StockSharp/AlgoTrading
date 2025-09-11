# Martin Strategy - No Loss Exit V3
[Русский](README_ru.md) | [中文](README_cn.md)

This martingale averaging strategy adds to a long position whenever price drops by a configured percentage from the first entry. Each new order increases the cash amount by a multiplier and adjusts the average price. The position is closed when the candle high reaches the average price plus the take-profit percentage, ensuring exits only in profit.

## Details

- **Entry Criteria**:
  - **Long**: `Flat` → buy for `Initial Cash`
  - **Add**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` && `orderCount < MaxOrders`
- **Long/Short**: Long only
- **Exit Criteria**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **Stops**: No
- **Default Values**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **Filters**:
  - Category: Averaging down
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
