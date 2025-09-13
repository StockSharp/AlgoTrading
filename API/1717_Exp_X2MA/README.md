# Exp X2MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Exp X2MA Strategy trades turning points of a double smoothed moving average.
Price is first smoothed with a simple moving average and then with a Jurik moving average.
When the smoothed line forms a local minimum the strategy buys and closes shorts.
When it forms a local maximum the strategy sells and closes longs.
Optional fixed stop loss and take profit protect open positions.

## Details
- **Data**: Price candles (default 4-hour).
- **Entry Criteria**:
  - **Long**: Previous X2MA value is lower than the older one and current value turns up.
  - **Short**: Previous X2MA value is higher than the older one and current value turns down.
- **Exit Criteria**: Opposite extreme, stop loss or take profit.
- **Stops**: Fixed stop loss and take profit in points.
- **Default Values**:
  - `FirstMaLength` = 12
  - `SecondMaLength` = 5
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
- **Filters**:
  - Category: Trend reversal
  - Direction: Long & Short
  - Indicators: SMA, JurikMovingAverage
  - Stops: Yes
  - Complexity: Low
  - Risk level: Medium
