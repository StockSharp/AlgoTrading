# Sophia 1_1 Strategy

Sophia 1_1 is a grid-based martingale trading strategy.
The strategy opens a position after four consecutive candles move in the same direction:
- Four rising candles trigger a short entry.
- Four falling candles trigger a long entry.

Once in the market, the algorithm adds positions every time the price moves against the current position by a fixed number of price steps (`Pip Step`).
The volume of each additional trade is multiplied by `Lot Exponent`, forming a classic martingale grid.

Risk management is handled through `Take Profit`, `Stop Loss` and an optional trailing stop.
The trailing mechanism starts after the profit reaches `Trail Start` and trails the stop level by `Trail Stop` price steps.

## Parameters
- **Volume** – base volume for the first trade.
- **Pip Step** – distance in price steps before adding a new position.
- **Lot Exponent** – multiplier for the volume of each additional trade.
- **Max Trades** – maximum number of positions in the grid.
- **Take Profit** – profit target in price steps from the average entry price.
- **Stop Loss** – loss threshold in price steps from the average entry price.
- **Use Trailing** – enable or disable the trailing stop.
- **Trail Start** – profit required before the trailing stop becomes active.
- **Trail Stop** – distance of the trailing stop in price steps.
- **Candle Type** – timeframe of the candles used for calculations.
