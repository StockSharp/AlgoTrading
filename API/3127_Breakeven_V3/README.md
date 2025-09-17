# Breakeven v3 Manager

## Overview
Breakeven v3 Manager is a conversion of the MetaTrader 5 expert advisor `Breakeven v3 (barabashkakvn's edition)`.
The original script does not open trades. Instead it continuously computes the portfolio break-even level for the
selected symbol and moves protective orders (stop-loss or take-profit) for every open long and short position
so that the whole book is closed around that break-even price with an optional buffer.

## Strategy logic
* **Break-even reconstruction** – each time a trade fills or new quotes arrive, the strategy rebuilds the weighted
  average open price for long and short exposure separately. It includes the per-position commissions that StockSharp
  reports in the `MyTrade` objects to mirror the MQL implementation.
* **Target price calculation** – the break-even price is shifted by `Delta (points)` MetaTrader points. The shift is
  added when the net exposure is long and subtracted when it is short, replicating the original "Delta" parameter.
* **Protective order placement** –
  * When the net exposure is long, a **sell limit** take-profit is placed for the total long volume and a **buy stop**
    stop-loss is attached to the aggregate short volume at the same price.
  * When the net exposure is short, a **buy limit** take-profit is placed for the full short volume and a **sell stop**
    stop-loss protects any long hedges.
  * If both sides are flat, all protective orders are cancelled.
* **Quote monitoring and diagnostics** – the strategy subscribes to Level1 updates. The latest bid/ask are used to
  compute distance-to-target statistics and an estimated floating profit. When `Enable Logging` is true these values
  are written to the strategy log to emulate the on-chart comments of the MQL version.

## Parameters
* **Delta (points)** – offset applied to the calculated break-even price. The value is expressed in MetaTrader points,
  i.e. one-tenth of a pip on five-digit FX symbols. Default: `100`.
* **Enable Logging** – toggles detailed log output describing the current break-even level, distance to target and
  floating PnL. Default: `true`.

## Usage notes
* The strategy is a trade manager. It should be launched on top of an existing strategy or manual position. It will not
  open market orders by itself.
* On start the code inspects the portfolio and reconstructs a single synthetic lot for each side of the position using
  the average price reported by StockSharp. For best accuracy keep the strategy running whenever new trades are opened.
* Swap charges are not available from StockSharp, therefore only the commission information is included when rebuilding
  the break-even price. If the broker applies overnight swaps they must be handled manually.
* The script assumes the account allows hedging (simultaneous long and short positions). If the broker nets positions,
  the long and short aggregates will reduce to a single net exposure just like in MetaTrader.
* There is no Python version of this port. Only the C# implementation is provided.
