# Virtual Trailing Stop

This strategy emulates a virtual trailing stop for both long and short positions. It does not generate entry signals; orders should be opened externally or manually. Once a position exists, the strategy maintains a trailing stop that follows price as it moves in a favorable direction. If price hits the stop level, the position is closed by market.

## Parameters

- `StopLoss` – fixed stop-loss distance in price steps.
- `TakeProfit` – fixed take-profit distance in price steps.
- `TrailingStop` – distance from current price to the trailing stop.
- `TrailingStart` – minimal profit in price steps before trailing begins.
- `TrailingStep` – minimal additional profit required to move the trailing level.
- `CandleType` – candle series used to process price data.

## Notes

The strategy subscribes to candles of the specified type and evaluates the trailing logic on closed candles only.
