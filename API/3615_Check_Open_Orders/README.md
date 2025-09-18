# Check Open Orders Strategy

## Overview
The Check Open Orders strategy is the StockSharp conversion of the MetaTrader 4 expert *Check_Open_Orders.mq4*. The original EA
was written as a tutorial utility: when the robot starts it immediately fires a few market orders and reports via the chart
comment whether there are any open positions that match the selected filter (all orders, only buys or only sells). The StockSharp
port keeps the same behaviour while leveraging the framework's high-level API for order submission, position tracking and
status reporting.

At start-up the strategy normalises the requested lot size to the instrument's volume constraints, subscribes to level 1 quotes
and asynchronously sends two buy market orders followed by one sell order. Between each submission it waits for the amount of
milliseconds configured by `WaitTimeMilliseconds`. After every fill the code logs a message identical in spirit to the original
`Comment()` output: it states which option was selected and whether matching positions remain open. Protective stop-loss and
take-profit levels are applied to each demonstration trade whenever bid/ask information is available.

## Trading logic
1. Load the execution parameters from StockSharp strategy parameters and align the trading volume with the security's minimum,
   maximum and step size.
2. Subscribe to level 1 quotes so that bid/ask updates are available for building protective orders.
3. Launch an asynchronous workflow that sends two buy market orders and one sell order. Each submission is separated by
   `WaitTimeMilliseconds` (converted to a `TimeSpan`).
4. After sending a market order, compute the reference price using the latest trade price or the most recent bid/ask pair and
   call `SetStopLoss` and `SetTakeProfit` with the distances expressed in broker points.
5. Recompute the textual status whenever a new trade arrives or the aggregated position changes. The message states the selected
   filter (`Mode`) and whether there are any outstanding positions that satisfy it.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.01` | Lot size used when submitting the sample market orders. Adjusted automatically to volume limits. |
| `StopLossPoints` | `decimal` | `100` | Stop-loss distance in broker points applied to every sample order. |
| `TakeProfitPoints` | `decimal` | `400` | Take-profit distance in broker points applied to every sample order. |
| `SlippagePoints` | `decimal` | `7` | Informational slippage buffer converted to price units and assigned to `Strategy.Slippage`. |
| `WaitTimeMilliseconds` | `int` | `2000` | Delay in milliseconds between sequential sample orders. |
| `Mode` | `CheckOpenOrdersMode` | `CheckAllTypes` | Determines whether the status line checks all net positions, only long exposure or only short exposure. |

The `CheckOpenOrdersMode` enumeration provides the following options:

- `CheckAllTypes` – report true when the strategy holds any non-zero net position.
- `CheckOnlyBuy` – report true only when the net position is long (positive volume).
- `CheckOnlySell` – report true only when the net position is short (negative volume).

## Differences versus the MetaTrader version
- StockSharp manages orders and positions natively, therefore the conversion does not need the MetaTrader `MagicNumber` field
  to filter trades; by design the strategy only inspects its own net position.
- Protective orders are placed with `SetStopLoss`/`SetTakeProfit` using the latest level 1 prices. If no bid/ask is available the
  strategy skips these calls and logs a warning, instead of falling back to raw `Bid`/`Ask` globals.
- Waiting between orders uses `Task.Delay` so the strategy remains responsive to new events, instead of blocking the main thread
  with `Sleep()`.
- Status information is sent through `AddInfoLog`, which appears in the log window and Designer UI rather than the chart comment.

## Usage notes
- Ensure the connected data provider delivers level 1 prices; without them the sample orders will still be sent but protective
  stops and targets are omitted.
- Because the conversion works with the aggregated position, hedged accounts that allow long and short tickets simultaneously
  will be reported according to the resulting net exposure.
- The default parameters match the tutorial values from the MetaTrader script. You can increase `WaitTimeMilliseconds` if the
  broker enforces a minimum delay between orders.
- The strategy is intentionally simple and meant for educational purposes. Remove the order-sending routine when reusing the code
  as a building block inside production systems.
