# GBP 9 AM Pending Orders Strategy

This strategy is a StockSharp conversion of the original MetaTrader 4 expert located in `MQL/7687/Gbp9am.mq4`. It recreates the London 9 AM breakout routine that arms two pending orders around the current price and keeps at most one active trade during the session.

## Trading idea

1. At the configured *look hour* and *minute* the strategy takes the latest candle close as a price snapshot.
2. A buy stop is placed above the snapshot price and a sell stop is placed below it. Both orders share the same volume and carry individual stop-loss levels together with a shared take-profit distance.
3. When one of the orders is filled the other is cancelled immediately so that only one position is ever active.
4. The open position is managed with synthetic stop-loss and take-profit levels that are checked on every completed candle.
5. A daily close-out hour can be enabled to flatten any remaining exposure and remove pending orders after the London session.
6. If both pending orders are removed without a trade, or the market time moves away from the observation hour, the strategy re-arms the next day exactly like the MetaTrader version.

The pip offsets are approximated using the instrument price step. If the broker provides fractional pips (3 or 5 decimal digits) the logic automatically scales to typical 0.1 pip increments.

## Parameter reference

| Parameter | Description |
|-----------|-------------|
| `Volume` | Order volume (lots) shared by both pending orders. |
| `LookHour` | Exchange hour that represents 9 AM London time. |
| `LookMinute` | Minute inside the look hour when the snapshot is taken. |
| `CloseHour` | Hour when all positions and pending orders are forcefully closed. |
| `UseCloseHour` | Enables or disables the daily close-out procedure. |
| `TakeProfitPips` | Target distance in pips, applied symmetrically to both directions. |
| `BuyDistancePips` | Offset in pips between the snapshot price and the buy stop entry. |
| `SellDistancePips` | Offset in pips between the snapshot price and the sell stop entry. |
| `BuyStopLossPips` | Stop-loss distance in pips for the long trade. |
| `SellStopLossPips` | Stop-loss distance in pips for the short trade. |
| `CandleType` | Candle series used for timing and stop management (default 1 minute). |

## Behavioural notes

- The strategy ignores unfinished candles to avoid multiple triggers inside the same bar.
- Order prices are rounded to the nearest valid tick using the security price step.
- The re-arming gate mirrors the `clear_to_send` flag of the MQL expert: once the daily straddle is placed no new orders are sent until either both pending orders disappear while the market is outside the look hour or the clock reaches the hour preceding the next signal.
- When `UseCloseHour` is enabled the strategy exits any open trade with a market order and clears pending orders once the close hour is reached.
- The pip calculations rely on historical candles, therefore the exact stop/target distances may differ slightly from the tick-based MetaTrader environment, especially on symbols with large spreads.

## Risk management

The conversion keeps the original static stops and targets. There is no trailing stop or scaling logic. Position protection is activated in `OnStarted` so that unexpected disconnections do not leave the account unguarded.

## Usage

1. Configure the `Volume`, `LookHour`, and `LookMinute` values to match the exchange timezone of your data feed.
2. Adjust the distance parameters to reflect the spread structure of your broker.
3. Attach the strategy to a GBPUSD symbol (or another FX pair of your choice) and start it before the London session.
4. Monitor the resulting trades in the StockSharp chart that is automatically drawn after start.

The implementation follows the guidelines from `AGENTS.md`: it uses the high level candle subscription API, employs strategy parameters with UI metadata, and avoids low-level history polling.
