# Pending Orders By Time 2 Strategy

## Overview
This strategy reproduces the behavior of the original MetaTrader "Pending orders by time 2" expert by scheduling breakout-style entry orders around a configurable opening hour. At the start of the trading session the algorithm places both a buy stop above the current ask price and a sell stop below the current bid price. Each pending entry carries its own stop-loss and take-profit levels expressed in instrument price steps, and once an entry fires the strategy maintains the open position with trailing stop logic and mutually exclusive exit orders. The code is designed for the StockSharp high-level API and uses tabs for indentation as required by the project guidelines.

## Trading Session Flow
1. **Daily reset** – On the first completed candle of a new trading day the strategy clears internal flags so a fresh pair of pending orders can be issued later in the session.
2. **Opening hour placement** – When the hour of the candle equals the configured opening hour, and orders have not yet been placed for the current day, the strategy calculates breakout prices relative to the latest best bid/ask snapshot (falls back to the candle close if no quotes are available) and submits both buy stop and sell stop orders.
3. **Intraday management** – While the session is active the logic trails the protective stop for any open position, keeps the opposite pending entry live (allowing a potential reversal), and waits for either the trailing stop, the fixed take-profit, or the opposite breakout order to close the position.
4. **Closing hour cleanup** – As soon as the candle hour matches the configured closing hour, the strategy cancels any still-active pending entry orders and closes the net position at market, ensuring no trades are carried overnight.

## Order Placement Details
- **Distance, stop-loss, take-profit** – The parameters `DistanceTicks`, `StopLossTicks`, and `TakeProfitTicks` are interpreted in instrument price steps (`Security.PriceStep`). The buy stop price is `bestAsk + DistanceTicks * step`, its stop-loss is placed `StopLossTicks` below the entry price, and the take-profit is the same distance above. The sell stop mirrors this logic on the short side.
- **Bid/ask handling** – The strategy subscribes to the order book and continuously records the latest best bid and ask. If the order book has not yet provided a quote, the close price of the finished candle is used as a safe fallback.
- **Order references** – References to the submitted pending orders are stored so the algorithm can cancel or re-register them when the session changes or when the closing hour triggers.

## Position & Risk Management
- **Protective orders** – When a pending entry fills (detected in `OnOwnTradeReceived`), the strategy immediately registers a protective stop order and a take-profit order with the original position volume. Long positions receive a `SellStop` and `SellLimit`, while short positions receive a `BuyStop` and `BuyLimit`. Only one stop and one take-profit order remain active at any given time; issuing new protective orders automatically cancels the previous pair.
- **Trailing stop** – Trailing is controlled by `TrailingStopTicks` (the actual stop distance) and `TrailingStepTicks` (minimum profit required before an adjustment). The trailing logic triggers once unrealized profit exceeds `TrailingStop + TrailingStep`. It recalculates a better stop price (never loosening the current stop), cancels the previous protective stop order, and submits a new one at the tighter level.
- **Closing hour exit** – When the closing hour arrives, the strategy cancels both protective orders and sends a market order sized to the absolute position so that no exposure remains open.

## Parameters
- `OpeningHour` – Hour (0–23) when the pending orders are created.
- `ClosingHour` – Hour (0–23) when pending orders are removed and positions are closed.
- `DistanceTicks` – Breakout distance from the current bid/ask expressed in price steps.
- `StopLossTicks` – Fixed protective distance for the initial stop.
- `TakeProfitTicks` – Fixed distance for the profit target.
- `TrailingStopTicks` – Distance maintained by the trailing stop once activated.
- `TrailingStepTicks` – Minimum additional profit required before the trailing stop is moved again.
- `Volume` – Size of both pending orders.
- `CandleType` – Timeframe used for session tracking and signal evaluation (defaults to 15-minute time frame).

## Implementation Notes
- Uses the StockSharp high-level `Strategy` API with `SubscribeCandles` and `SubscribeOrderBook` bindings; no low-level indicator access is required.
- `OnOwnTradeReceived` is leveraged to keep protective orders synchronized with the filled entry order and to clean up when either the stop-loss or take-profit executes.
- The trailing logic deliberately avoids calling indicator `GetValue` and relies only on the incoming candle and stored state, complying with conversion guidelines.
- Distances are based on price steps, mirroring the original pip-based arithmetic from the MQL implementation and remaining instrument-agnostic.
- Python implementation is intentionally omitted per the task requirements; only the C# version is provided in this folder.
