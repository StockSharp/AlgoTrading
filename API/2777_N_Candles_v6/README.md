# N Candles v6 Strategy

## Overview
The **N Candles v6** strategy monitors the most recent finished candles and looks for streaks of identical direction. When the market prints `N` bullish candles in a row the strategy opens a long position, while a string of `N` bearish candles produces a short entry. The logic is inspired by the MetaTrader expert advisor *N Candles v6.mq5* and is adapted to the StockSharp high-level API.

The algorithm is designed for any symbol that delivers standard time-based candles. A configurable trading window keeps the strategy inactive outside of the desired session, but active trailing and exit logic continues to protect an open position even during the blocked hours.

## Trading Logic
1. Subscribe to the configured candle type and process only finished candles.
2. Count consecutive bullish (`Close > Open`) and bearish (`Close < Open`) candles. Dojis reset the counters.
3. When `CandlesCount` bullish candles appear:
   - Verify that the projected net position stays below `MaxPositionVolume`.
   - Send a market buy order. If a short position exists, the order size is increased to flip the position long in one trade.
4. When `CandlesCount` bearish candles appear:
   - Ensure the new short exposure will not exceed `MaxPositionVolume`.
   - Send a market sell order and enlarge the order if a long position must be closed.
5. If the newest candle breaks the streak (the “black sheep”):
   - Apply the selected `ClosingMode` to close all, opposite, or same-direction positions once.
6. Trailing and protective exits run on every candle:
   - Stop-loss and take-profit levels are derived from pip distances and the instrument price step.
   - The trailing stop activates after price moves by `TrailingStopPips + TrailingStepPips` and only ratchets in the favorable direction.
   - Any breach of the stop, take-profit, or trailing level closes the entire position immediately.

## Risk Management
- **Stop Loss (pips)** – converts pip distance into an absolute price offset using the symbol price step (5- and 3-digit instruments are automatically scaled).
- **Take Profit (pips)** – closes the position after a favorable move of the specified size.
- **Trailing Stop / Step (pips)** – enables dynamic protection once the trade reaches the configured profit threshold. The step must be non-zero when trailing is active.
- **Max Position Volume** – caps the absolute net position. Signals that would breach the cap are ignored.
- **Closing Mode** – determines how to react when a non-conforming candle appears:
  - `All` – flat the entire position.
  - `Opposite` – close positions against the streak direction (e.g., close shorts after bullish run breaks).
  - `Unidirectional` – close positions in the streak direction only.
- **Trading Window** – the strategy opens new trades only when the candle open time hour lies between `StartHour` and `EndHour` (inclusive). Protective exits continue to operate even when new trades are blocked.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandlesCount` | 3 | Number of identical candles required for a signal. |
| `OrderVolume` | 0.01 | Base market order size. Opposite exposure is closed before establishing a new trade. |
| `TakeProfitPips` | 50 | Take-profit distance in pips. `0` disables the target. |
| `StopLossPips` | 50 | Stop-loss distance in pips. `0` disables the stop. |
| `TrailingStopPips` | 10 | Trailing stop distance in pips. `0` disables trailing. |
| `TrailingStepPips` | 4 | Minimum price improvement before the trailing level moves. Must be > 0 when trailing is enabled. |
| `MaxPositionVolume` | 2 | Maximum absolute net position. |
| `UseTradingHours` | true | Enables trading window filtering. |
| `StartHour` | 11 | Beginning of the trading session (0-23). |
| `EndHour` | 18 | End of the trading session (0-23). |
| `ClosingMode` | All | Behaviour when a black sheep candle appears. |
| `CandleType` | 1 hour candles | Data type used for signal generation. |

## Notes
- The pip conversion is based on the instrument `PriceStep`. For 5- and 3-digit quotes the strategy multiplies the step by ten to match the traditional pip definition.
- Call `StartProtection()` during startup to enable StockSharp safeguard services (cancel-on-stop, reconnection safety, etc.).
- The logic uses the net position (`Strategy.Position`) and therefore operates correctly on netting accounts. Hedging-style behaviour can be emulated by setting a high `MaxPositionVolume`.
