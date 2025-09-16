# Ambush Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Ambush strategy continuously surrounds the market with a pair of buy-stop and sell-stop orders. The pending orders are placed
at a configurable indentation above the best ask and below the best bid, with a dynamic override that enforces a minimal distance
based on the current spread. Whenever one side is triggered the strategy immediately rebuilds both orders so that the market stays
"ambushed" from both directions. A simple equity-based circuit breaker can flatten positions once a daily profit target or loss
limit is reached.

This C# implementation replicates the behaviour of the original MetaTrader 5 expert by Zuzabush. It operates purely on Level 1
quotes and does not require candles or indicators. Every decision is driven by real-time bid/ask changes, so the strategy is best
suited for liquid instruments with tight spreads.

## Trading Logic

1. **Market data intake**
   - The strategy subscribes to Level 1 updates and tracks the latest best bid and best ask.
   - Calculations stop until both sides of the order book are available and the strategy is allowed to trade.
2. **Equity safeguards**
   - The realised PnL (`PnL`) and the unrealised component derived from the current bid/ask and `PositionPrice` are summed.
   - If the combined equity exceeds `EquityTakeProfit`, or drops below `-EquityStopLoss`, the current net position is flattened
     with a market order. Pending orders are left intact, matching the original expert behaviour.
3. **Pending order placement**
   - Spread in price units is compared with `MaxSpreadPoints`. If the spread is too wide, no new orders are placed.
   - Otherwise a distance is calculated as `max(IndentationPoints * step, spread * 3)`. That value replicates the MT5 logic of
     either respecting the user indentation or enforcing three spreads when the broker `StopsLevel` is zero.
   - A buy-stop order is placed at `ask + distance` and a sell-stop at `bid - distance`. Prices are normalised to the nearest
     tick. Only one active order per side is allowed; stale orders are cleaned up when their state transitions to `Done`,
     `Failed`, or `Canceled`.
4. **Trailing of pending orders**
   - When `TrailingStopPoints` is greater than zero, the strategy periodically (no more frequently than `Pause`) recalculates the
     stop distance using `max((TrailingStopPoints + TrailingStepPoints) * step, spread * 3)` and re-registers the orders if the
     change exceeds half a tick.
   - Trailing keeps the orders close to the market while still respecting the minimum distance that avoids premature triggering.

The end result is a grid-like breakout engine that is constantly waiting for price to move decisively in either direction.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `IndentationPoints` | Base distance in points between the market and each pending stop order. |
| `MaxSpreadPoints` | Maximum allowed spread (in points). Orders are suspended while the spread is wider. |
| `TrailingStopPoints` | Base trailing distance in points applied to existing pending orders. Set to zero to disable trailing. |
| `TrailingStepPoints` | Additional buffer added on top of the trailing base distance. |
| `Pause` | Minimum time between two trailing recalculations. The default mirrors the one-second pause from the MT5 expert. |
| `EquityTakeProfit` | Equity profit in account currency that triggers an immediate position flattening. |
| `EquityStopLoss` | Allowed equity drawdown before the open position is closed. |
| `Volume` | Order size inherited from the base `Strategy` class. Use the broker minimum to mimic the MT5 default. |

All price offsets are converted from points to actual price units using `Security.PriceStep`. If the instrument does not expose a
price step, a fallback value of 1 is used.

## Practical Notes

- Because the strategy works with stop orders only, no candles or indicators are required. It can run during backtests that do not
  provide historical candles as long as Level 1 data is available.
- Brokers that enforce a non-zero `StopsLevel` should configure `IndentationPoints` so that the resulting price difference satisfies
  the exchange rule. The triple-spread safety net acts as a secondary guard.
- The equity filter is intentionally light-touch and does not cancel pending orders. This mirrors the original Ambush behaviour,
  allowing new trades after the flattening event without manual intervention.
- Slippage and order fill tolerance are controlled by the connected broker or simulator. Adjust `Volume` and parameter values to
  match the instrument volatility.

This documentation intentionally provides the maximum level of detail so that both discretionary and algorithmic traders can
understand the conversion and customise the strategy for their execution venue.
