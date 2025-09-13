# Charles 1.3.7 Strategy

This strategy places symmetric stop orders above and below the current price and uses trailing exits to capture breakouts.

## Parameters

- **Anchor** – distance in price steps to place stop orders.
- **XFactor** – multiplier for the order volume.
- **Trailing Stop** – trailing stop distance in price steps.
- **Trailing Profit** – profit threshold to exit the position.
- **Stop Loss** – fixed stop loss in price steps (0 disables it).
- **Volume** – base order volume.
- **Candle Type** – timeframe of processed candles.

## Trading Logic

1. When no position is open, cancel existing orders and place both a Buy Stop and Sell Stop at `Anchor` steps from the last candle close.
2. When a position is opened, the opposite stop order is cancelled. The entry price is remembered for exit calculations.
3. For a long position, if profit reaches `Trailing Profit` or the price falls by `Stop Loss`, the position is closed. For a short position, the logic is mirrored.

The strategy is designed as an example of breakout trading with simple risk management.
