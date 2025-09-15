# Re Open Positions

This strategy is a StockSharp port of the MQL5 example `Exp_ReOpenPositions`. It demonstrates how to reopen positions when the current trade becomes profitable.

## Logic

1. The strategy opens an initial long position on start.
2. When price advances by `ProfitThreshold` points from the last entry price, it opens another long position.
3. Each new entry updates stop loss and take profit levels relative to its own price.
4. If price reaches the stop loss or take profit, all positions are closed and the cycle resets.

The same rules work for short trades if the first position is short.

## Parameters

- `ProfitThreshold` – price movement in points required to add a new position.
- `MaxPositions` – maximum number of opened positions.
- `StopLossPoints` – distance from entry to protective stop.
- `TakeProfitPoints` – distance from entry to target profit.
- `CandleType` – candle data type for processing.

## Notes

The example is simplified for educational purposes and does not manage trade volume or money management as in the original script.
