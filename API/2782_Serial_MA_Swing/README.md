# Serial MA Swing Strategy (API/2782)

## Summary
- Converts the MetaTrader SerialMA expert advisor into a StockSharp high-level strategy using candle subscriptions and a custom serial moving average indicator.
- Opens new swing positions whenever the serial moving average flips its direction relative to price, optionally reversing the signal and limiting the number of concurrent swings.
- Implements the same protective stop-loss and take-profit distances measured in instrument points, recalculated on every finished candle.

## Serial Moving Average indicator
The original EA depends on the custom *SerialMA* indicator that rebuilds its moving average after each price crossover. The ported indicator replicates this behaviour by:
1. Accumulating closing prices from the most recent crossover and calculating their arithmetic mean.
2. Tracking the difference between the mean and the current close to detect a sign change.
3. Resetting the internal window whenever the sign changes, effectively restarting the average from the crossover bar and flagging the event for the strategy.

This implementation exposes the moving average value together with a boolean flag indicating that a crossover occurred on the previous bar, allowing the strategy to mirror the MQL logic without manual buffer access.

## Trading logic
1. On every finished candle the strategy reads the serial moving average value and the crossover flag.
2. When the previous candle triggered a crossover:
   - If the previous close was above the previous moving average, a long signal is generated.
   - If the previous close was below the previous moving average, a short signal is generated.
3. The **ReverseSignals** parameter optionally swaps long and short entries.
4. The **OpenedMode** parameter controls position stacking:
   - **AllSwing** opens a new order on every signal, even if a position already exists in that direction.
   - **SingleSwing** only opens a new order when no exposure exists in that direction.
5. Before submitting a new order the strategy always closes existing exposure in the opposite direction to keep the swing logic consistent with the source EA.
6. Stop-loss and take-profit distances are applied on each candle using the instrument price step, matching the point-based risk controls from the original expert.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OpenedMode` | Allows either stacking swings or keeping a single swing per direction. | `AllSwing` |
| `EnableBuy` | Enables or disables long entries. | `true` |
| `EnableSell` | Enables or disables short entries. | `true` |
| `ReverseSignals` | Inverts the trading direction. | `false` |
| `TradeVolume` | Order size (lots) for each new swing. | `1` |
| `StopLossPoints` | Stop-loss distance in price steps (points). A value of `0` disables the stop. | `0` |
| `TakeProfitPoints` | Take-profit distance in price steps (points). A value of `0` disables the take profit. | `0` |
| `CandleType` | Candle data type used for calculations. | `5 minute candles` |

## Order management and protection
- When long, the strategy checks whether the candle low violated the stop-loss level or the candle high reached the profit target and issues a market order to flatten accordingly.
- When short, the candle high triggers the stop-loss and the candle low triggers the profit target.
- Protective levels are measured in `PriceStep` units. If the instrument does not provide a price step the protective checks remain idle, mirroring the behaviour of missing tick size information.

## Usage notes
- The implementation uses the StockSharp high-level API (`SubscribeCandles` + `BindEx`) and avoids low-level buffer management.
- No Python version is included, as requested. Only the C# port resides in `CS/SerialMASwingStrategy.cs`.
- The strategy is intended for swing-style execution similar to the original EA; enabling both directions and keeping the default `AllSwing` mode most closely resembles the MQL behaviour.
