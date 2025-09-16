# VR Setka 3 Strategy

The **VR Setka 3 Strategy** implements a grid-based trading approach. The strategy places symmetrical buy and sell limit orders around the current market price. After an order is filled, the take-profit level is recalculated using the average entry price of all positions in the active direction. New grid orders are placed with increasing spacing and, optionally, with increasing volume (martingale).

## Parameters
- **Start Offset** – initial distance from the current price for the first pair of limit orders.
- **Take Profit** – distance from the average entry price where all positions are closed for profit.
- **Grid Distance** – base step between grid levels.
- **Step Distance** – additional distance added for each subsequent grid level.
- **Use Martingale** – when enabled, each new grid order increases its volume using the multiplier.
- **Martingale Multiplier** – factor for volume increase when martingale is active.
- **Volume** – base order volume for the first level.
- **Candle Type** – timeframe used to synchronize strategy operations.

## Algorithm
1. At start, the strategy places a **buy limit** below and a **sell limit** above the current price.
2. When one side is filled, the opposite order is cancelled.
3. The strategy recalculates a common take-profit level at the average price ± *Take Profit*.
4. If the price moves against the position, a new limit order is placed at **Grid Distance + Step Distance × level** from the average price. Volume increases if martingale is enabled.
5. When price reaches the take-profit level, all positions in that direction are closed and the grid is reset.

## Notes
- The strategy does not open positions in both directions simultaneously.
- Proper risk management is required because martingale can quickly increase position size.
- Works with any instrument supported by StockSharp as long as the chosen candle type is available.
