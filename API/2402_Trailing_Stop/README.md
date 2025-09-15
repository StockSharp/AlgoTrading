# Trailing Stop Strategy

This strategy manages trailing stop levels for existing positions. It does not generate entry signals. Instead, it continuously adjusts stop levels as the market moves in favor of an open position. When price moves against the position and crosses the calculated stop level, the position is closed with a market order.

## How it works

1. Subscribe to candles of the selected timeframe.
2. For long positions:
   - Calculate the stop price as `close - trailingStop * priceStep`.
   - Move the stop only in the profitable direction.
   - If the candle low is below the stop level, close the position.
3. For short positions:
   - Calculate the stop price as `close + trailingStop * priceStep`.
   - Move the stop only in the profitable direction.
   - If the candle high is above the stop level, close the position.

The strategy resets internal stop values whenever no position exists.

## Parameters

- `TrailingStop` — trailing distance in points.
- `CandleType` — candle timeframe used for price updates.

## Notes

- The strategy is intended to be used together with other entry strategies or manual trades.
- All comments in the code are provided in English.

