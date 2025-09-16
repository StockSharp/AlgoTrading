# Doji Arrows Strategy

## Concept
The Doji Arrows strategy converts the original MetaTrader "Doji Arrows" expert advisor into the StockSharp high-level API. The idea is to wait for a genuine doji candle and then trade a breakout of its range. A doji candle represents indecision, therefore a close above the doji high hints at bullish strength while a close below the doji low indicates bearish control.

1. The strategy processes only completed candles from the configured `CandleType` subscription.
2. The previous candle is analysed to determine whether it is a doji. The candle is classified as a doji when the absolute difference between the open and close is less than or equal to `DojiBodyPoints` multiplied by the security price step. If the parameter is set to `0`, a single price step is used as tolerance which matches the strict equality check in the MQL5 version.
3. When the next candle closes above the doji high, the strategy sends a market buy order. When the next candle closes below the doji low, a market sell order is issued. Existing opposite positions are flattened automatically by the market order volume.

This sequence mirrors the original expert advisor that reacted once at the opening of each new bar.

## Risk Management
The converted implementation keeps the protective behaviour of the MQL script:

- **Stop loss**: `StopLossPoints` controls how far, in price steps, the initial stop loss is placed from the entry price. Set to zero to disable the fixed stop.
- **Take profit**: `TakeProfitPoints` defines the distance to the profit target in price steps. Set to zero to skip the target.
- **Trailing stop**: `TrailingStopPoints` and `TrailingStepPoints` reproduce the trailing mechanism. Once the trade gains more than `TrailingStopPoints + TrailingStepPoints`, the stop level is pulled to `TrailingStopPoints` away from the latest close (highest close for long, lowest close for short). Trailing is optional and activates only when `TrailingStopPoints` is greater than zero.

Stops and targets are evaluated on every finished candle. When any level is breached (using the candle high/low), the strategy exits the position with a market order and resets the protection state.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `StopLossPoints` | `30` | Distance of the initial stop loss in price steps. |
| `TakeProfitPoints` | `90` | Distance of the take profit in price steps. |
| `TrailingStopPoints` | `15` | Distance used by the trailing stop in price steps. |
| `TrailingStepPoints` | `5` | Extra profit required before the trailing stop is adjusted, in price steps. |
| `DojiBodyPoints` | `1` | Maximum allowed body size of the previous candle in price steps to treat it as a doji. `0` uses one price step as tolerance. |
| `CandleType` | `1 hour` | Candle type subscribed for signal generation. |

## Implementation Notes
- The strategy subscribes to candles through `SubscribeCandles(CandleType).Bind(ProcessCandle)` and keeps only the latest completed candle in memory.
- The security price step is retrieved via `Security?.PriceStep`. When it is unavailable, a fallback value of `1` is used so that the strategy can still operate on synthetic or historical data.
- Protective levels are recalculated after every entry, and the trailing logic can create a stop even when the fixed stop loss is disabled (matching the MQL behaviour where the trailing stop could start from zero).
- All actions are executed with market orders to stay aligned with the original advisor that relied on immediate market execution.

## Usage Tips
1. Configure the `Security`, `Portfolio` and `Volume` properties before starting the strategy.
2. Adjust the point-based parameters according to the traded instrument. For instruments quoted with fractional pips, increase the values to match the broker tick size.
3. Combine the strategy with StockSharp risk controls or analytics modules if more advanced position sizing is required, because the conversion keeps the fixed-volume logic of the original code.
