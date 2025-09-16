# HelloSmart Strategy

This strategy implements a simple grid trading approach that opens positions in only one direction. A new order is placed each time the market moves a configured number of ticks against the last entry. When the aggregate position volume reaches a threshold, the next order size is multiplied. All positions are closed when total profit or loss hits predefined limits.

## Parameters
- **Trade Direction** – choose 1 to open only long positions or 2 to open only short positions.
- **Step** – number of price ticks the market must move before adding another position.
- **Initial Lot** – base volume for the first order.
- **Threshold Volume** – cumulative position size that triggers lot multiplication.
- **Maximum Lot** – upper bound for any single order volume.
- **Profit Target** – profit amount in currency after which all positions are closed.
- **Loss Limit** – loss amount in currency after which all positions are closed.
- **Lot Multiplier** – factor applied to the next order when the threshold volume is exceeded.
- **Candle Type** – candle series used to measure price movement.
