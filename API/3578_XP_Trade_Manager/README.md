# XP Trade Manager Strategy

## Overview
The **XP Trade Manager Strategy** is a direct conversion of the MetaTrader expert advisor “XP Trade Manager”.
It does not open trades by itself; instead, it supervises manually opened positions and automatically manages
stop-loss, take-profit, trailing stop, and break-even logic. The strategy supports an optional *stealth mode*
that keeps all exits internal to the strategy without registering protective orders on the exchange.

The implementation uses StockSharp’s high-level API, listens to Level1 updates, and reacts whenever positions
change or new price data arrives. All protective actions are aligned with the pip-based configuration used by
the original MQL script, including the special handling of 3- and 5-digit FX symbols.

## Workflow
1. Start the strategy on an instrument and open positions manually (or through another strategy).
2. Once a net position appears, the manager instantly places the configured stop-loss and take-profit levels
   (or stores them internally when stealth mode is enabled).
3. As price moves in favour of the position, the trailing controller periodically tightens the stop according to
   the configured start threshold, step, and distance.
4. If trailing is disabled, a break-even controller can raise the stop to the entry price plus a lock-in offset
   after a specified profit threshold.
5. When stealth mode is active, the strategy closes the position via market orders once price touches the
   internally tracked stop-loss or take-profit level.
6. Closing the position (manually or via protection) clears all protective orders and resets the controller.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `StopLossPips` | Initial stop-loss distance in pips. Set to 0 to disable the stop-loss. |
| `TakeProfitPips` | Initial take-profit distance in pips. Set to 0 to disable the take-profit. |
| `UseBreakEven` | Enables the break-even controller when trailing is disabled. |
| `BreakEvenActivationPips` | Profit in pips required before the break-even stop is applied. |
| `BreakEvenLockPips` | Profit kept when moving to break-even. This value is also used when the trailing stop is limited by `TrailingEndsAtBreakEven`. |
| `UseTrailingStop` | Enables the trailing stop controller. When true, break-even is ignored (as in the original EA). |
| `TrailingStartPips` | Profit in pips required before trailing activates. |
| `TrailingStepPips` | Profit increment that triggers a new trailing step. |
| `TrailingDistancePips` | Distance between current price and the trailing stop. |
| `TrailingEndsAtBreakEven` | Caps the trailing stop at the break-even lock level. Matches the *TSEndBE* flag in MQL. |
| `StealthMode` | Prevents protective orders from being sent. Stops and targets are enforced internally using market closes. |

## Behaviour Details
- Pip conversion follows the MetaTrader convention: for 3- and 5-digit quotes the strategy multiplies the price
  step by 10 before converting pips to price distance.
- Trailing logic mirrors the original EA: it activates only after `TrailingStartPips` and applies a floor of
  `priceDistance / TrailingStepPips`. Each new step shifts the stop by `TrailingDistancePips` behind the current
  price. When `TrailingEndsAtBreakEven` is enabled, the stop never surpasses the break-even lock level.
- Break-even logic is evaluated only when trailing is disabled, locking a profit of `BreakEvenLockPips` once
  `BreakEvenActivationPips` is reached.
- In stealth mode the strategy refrains from placing stop or limit orders. Instead, it watches bid/ask quotes and
  closes the current position at market when an internal level is breached. This reproduces the "hidden" stop
  behaviour of the original expert advisor.
- All protective orders are cancelled automatically when the net position returns to zero or the position side
  flips. The controller also resets when a new position opens so that trailing and break-even calculations start
  fresh.

## Usage Notes
- The strategy expects a valid `Security` with a populated `PriceStep`. Without it the pip conversion falls back
  to a step of 1.
- The manager must remain running for protection to work. Stopping the strategy cancels any active protective
  orders unless stealth mode is used.
- When running in stealth mode, make sure sufficient liquidity exists to close positions instantly; otherwise the
  behaviour may differ from a server-side stop.
- The original script displayed day profit statistics on the chart. This StockSharp port focuses on risk
  management and does not render chart labels.

## Migration Tips
- Parameter defaults mirror the MetaTrader version: 20 pip stop-loss, 40 pip take-profit, trailing enabled with
  a 10/10/15 structure, and break-even enabled with a 50-pip trigger and 10-pip lock.
- To reproduce the original “OnlyCurrentPair” behaviour simply attach the strategy to a single instrument; the
  StockSharp implementation operates on its assigned security only.
- When optimising, use the built-in `StrategyParam` metadata to expose the pip-based inputs to StockSharp’s
  optimiser.
