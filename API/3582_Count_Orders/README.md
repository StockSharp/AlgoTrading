# Count Orders Strategy

## Overview
This strategy is the StockSharp port of the MetaTrader expert advisor **Count_Orders.mq4**. The original robot was designed to
 demonstrate how many orders of each direction are currently open and printed the totals via `Comment()`. The StockSharp version
 keeps the same focus: it continuously reports the number of active buy and sell orders so you can monitor exposure at a glance.
 For convenience it also recreates the three demonstration orders that the MQL script fired at start, making the live counters
 immediately visible inside the strategy logs.

The algorithm does not rely on indicators or price patterns. All logic is event driven: volume normalisation happens once at
start, the sample trades are dispatched asynchronously, and the order book is watched via `OnOrderChanged` to keep the running
statistics up to date.

## Workflow
1. **Parameter alignment** – When the strategy starts it normalises the requested volume with respect to the security's
   `VolumeStep`, `VolumeMin`, and `VolumeMax`. Any adjustment is logged so you understand why the live volume might differ from
   the input value.
2. **Sample trade sequence** – Two buy market orders followed by one sell market order are submitted. An optional delay (default
   2000 ms) separates each submission, replicating the `Sleep()` calls from the MetaTrader script without blocking the strategy
   thread.
3. **Protective orders** – After each market entry, the code attempts to attach stop-loss and take-profit targets using the
   MetaTrader-style point distances. If there is no valid reference price yet, the protection step is skipped and a warning is
   written to the log.
4. **Order counting** – Whenever StockSharp notifies about an order state change, the strategy adds or removes that order from the
   appropriate hash set (buy or sell). The totals are written with `AddInfoLog`, mirroring the `Comment()` output of the original
   expert.
5. **Graceful shutdown** – A cancellation token aborts any pending delay so the asynchronous workflow stops immediately when the
   strategy is halted.

## Parameters
| Name | Description |
| --- | --- |
| **Magic Number** | Identifier copied into `Order.UserOrderId` for the sample trades. It mirrors the `MAGICMA` extern input from the
MQL version and lets you group the demonstration orders in reports. |
| **Stop-Loss Points** | Distance of the protective stop in MetaTrader points. A value of zero disables the stop attachment. |
| **Take-Profit Points** | Distance of the protective target in MetaTrader points. A value of zero disables the take-profit order. |
| **Trade Volume** | Lot size for each demonstration order. The value is normalised to the instrument's allowed range and step. |
| **Slippage** | Legacy parameter carried over from MQL. StockSharp's market orders ignore it but it is exposed for completeness. |
| **Wait Time (ms)** | Delay between the sample orders, expressed in milliseconds. Set to zero to fire all three orders back-to-back. |

## Implementation Details
- The class lives in the `StockSharp.Samples.Strategies` namespace and follows the high-level API guidelines. All indentation
  uses tabs as requested in the project instructions.
- `StartProtection()` is called once when the strategy starts so the helper methods `SetStopLoss` and `SetTakeProfit` can manage
  protective orders for the resulting position.
- A single `Task.Run` hosts the sample-order workflow. It calls `Task.Delay` with the configured wait time and observes a
  `CancellationTokenSource` that is cleared when `OnStopped` fires, preventing lingering background work.
- Active buy and sell orders are stored in two `HashSet<Order>` instances. The sets only track the strategy's own orders and
  exclude items that have already finished (done, failed, or cancelled) to keep the counters accurate.
- Reference prices for the protective targets are retrieved from best bid/ask quotes, falling back to the last trade when
  necessary. If no price is available yet the strategy logs a warning instead of guessing levels.

## Usage Notes
- Because the logic only depends on order events, the strategy does not subscribe to candles by default. Attach it to any
  connector and assign a security; the initial market orders will demonstrate the counting feature immediately.
- The sample orders are purely illustrative. Set **Trade Volume** to zero if you only want to use the counting feature without
  sending trades, or disable the entire sequence by stopping the strategy right after start.
- Protective orders require valid market data. On illiquid instruments the first few logs may warn that stop/limit levels were
  skipped until quotes arrive.

## Conversion Notes
- The MetaTrader function `OrdersCount()` has been replaced with an event-driven tracker using `OnOrderChanged`, which is more
  natural in StockSharp because orders already notify about their lifecycle.
- The `Comment()` output was translated into `AddInfoLog` messages. These appear in the strategy's diagnostic log and can be
  exported just like any other StockSharp log stream.
- Blocking `Sleep()` calls were converted to asynchronous delays, allowing the host application to remain responsive.
