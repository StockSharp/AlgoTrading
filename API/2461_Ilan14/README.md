# Ilan14 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Ilan14 is a hedging grid strategy that opens long and short positions simultaneously. When the market moves against one side by a user defined pip distance, the strategy adds a new order in that direction with its volume multiplied by the **Lot Exponent**. The average price of the position is tracked and once price reverts by the configured **Take Profit** distance, all orders on that side are closed.

Parameters:
- **Pip Step** – distance in pips between grid orders.
- **Lot Exponent** – multiplier applied to the volume of each additional order.
- **Max Trades** – maximum number of orders per direction.
- **Take Profit** – profit target in pips from the weighted average price.
- **Initial Volume** – volume of the first order.
- **Candle Type** – timeframe for candle subscription.

The implementation uses the high level StockSharp API with candle subscriptions and avoids manual data collections. Both sides of the grid are managed independently, allowing the strategy to capture rebounds after adverse moves.
