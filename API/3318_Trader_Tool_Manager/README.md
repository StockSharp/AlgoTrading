# TraderToolEA Manual Panel (StockSharp Port)

## Summary

The original MetaTrader 4 expert advisor **TraderToolEA v1.8** is not an autonomous trading robot but a
control panel that helps discretionary traders manage orders, grids and protective levels. This port
recreates the panel inside the StockSharp framework. Instead of on-chart buttons the strategy exposes
boolean parameters that behave like toggle buttons – set them to `true` in the GUI or in scripts to
trigger the corresponding action.

Key capabilities that were translated:

* Market order shortcuts for opening or closing long/short exposure.
* Automatic placement of symmetric grids made of stop or limit pending orders.
* Selective cancellation of pending orders (buy/sell/all) with optional orphan cleanup.
* Virtual stop-loss, take-profit, trailing stop and break-even management driven by Level1 quotes.
* Auto-sizing option that mimics the MetaTrader lot calculation (`AccountBalance / LotSize * RiskFactor`).

All logic relies exclusively on the high-level API: Level1 subscriptions, helper order methods
(`BuyStop`, `SellLimit`, `CancelOrder`…) and the built-in logging facilities.

## Parameters

| Name | Description |
| --- | --- |
| `Use Auto Volume` | When `true` the strategy calculates the lot size from the portfolio balance and `Risk Factor`; otherwise the fixed `Order Volume` is used. |
| `Risk Factor` | Multiplier applied to the portfolio balance during auto volume calculation. Equivalent to the MT4 `RiskFactor` input. |
| `Order Volume` | Manual lot size used for every market or pending order when auto sizing is disabled. |
| `Distance (pips)` | Spacing (in MetaTrader pips) between layered pending orders. Applies to both stop and limit grids. |
| `Layers` | Number of additional pending orders per command. `1` mirrors a single button press in the EA, higher values emulate multiple presses. |
| `Delete Orphans` | When enabled the strategy automatically cancels unmatched pending orders so that buy/sell grids stay balanced after partial executions. |
| `Enable Stop Loss` / `Stop Loss (pips)` | Activates fixed stop-loss monitoring measured in pips relative to the average entry price. |
| `Enable Take Profit` / `Take Profit (pips)` | Activates fixed take-profit monitoring measured in pips. |
| `Enable Trailing` / `Trailing (pips)` | Enables virtual trailing stop management. The trail only arms once the price moves at least `Trailing` pips in favor of the position. |
| `Enable Break-Even` / `Break-Even Trigger` / `Break-Even Lock` | Once the price advances by the trigger distance the stop is moved to the entry price plus the lock offset (longs) or minus the offset (shorts). |
| Command toggles (`Open Buy`, `Place Buy Stops`, `Delete Sell Limits`, …) | Boolean parameters that emulate the EA buttons. Setting them to `true` executes the action and the strategy resets them to `false`. |

## Order workflow

1. **Data feed** – the strategy only subscribes to `DataType.Level1`. Best bid/ask updates drive both the
   protection logic and the grid placements.
2. **Volume normalisation** – before submitting any order the requested volume is rounded to the
   instrument `VolumeStep` and clamped between `MinVolume` and `MaxVolume`. If the metadata is missing the
   raw value is used.
3. **Pending orders** – stop and limit grids are built around the most recent bid/ask. Prices are aligned
   to the instrument price step to avoid rejections from the matching engine.
4. **Orphan control** – when `Delete Orphans` is enabled the strategy keeps the number of buy and sell
   pending orders symmetrical by cancelling the excess side after fills or manual cancellations. The
   same logic is applied independently to stop and limit grids.
5. **Virtual protection** – stop-loss, take-profit, trailing stop and break-even are implemented as
   *virtual* guards. When a threshold is breached the strategy issues a closing market order for the
   remaining volume and resets the internal trailing/break-even state.

## Differences vs. MetaTrader implementation

* Graphical components (buttons, text boxes, colors, sounds) are replaced by StockSharp parameters and
  logs. Every action writes an informative entry through `AddWarningLog` or the default logger.
* Protective logic operates on Level1 updates and closes positions directly instead of modifying stop
  prices on individual orders. This keeps behaviour consistent across brokers that do not support MetaTrader-style stop orders.
* The `ManageOrders` modes from MT4 (ID/manual/all/own) collapse to the strategy scope: only orders
  created by this strategy are tracked and managed.
* Automatic lot sizing uses the portfolio valuation instead of `AccountBalance()`, but the formula and
  rounding rules are kept intact.

## Usage tips

1. Configure the instrument metadata (`PriceStep`, `VolumeStep`, `MinVolume`, `LotSize`, …) in your
   connection so that pip conversion and volume rounding match the broker rules.
2. Bind the boolean command parameters to hotkeys or UI buttons in the StockSharp terminal to replicate
   the original user experience. The properties reset to `false` after each successful invocation.
3. Enable `Delete Orphans` when working with symmetrical grids to ensure that leftover stops/limits are
   cleaned up automatically when one side is triggered.
4. Monitor the info log: if the strategy skips an action (for example because bid/ask is unavailable or
   the calculated volume is zero) a warning is produced with the reason.
5. Because protection is virtual, keep the strategy running while positions are open – it closes trades by
   sending market orders, not by relying on server-side stop orders.

## Porting notes

* The pip size mirrors MetaTrader: instruments with 3 or 5 decimals multiply the price step by 10 to
  transform points into pips.
* Trailing stops and break-even logic follow the MQL code flow: they only arm after the price moves into
  profit and use state variables that reset on new trades, cancellation or position reversal.
* The EA allowed pressing buttons multiple times to extend grids. The `Layers` parameter emulates that by
  creating multiple pending levels in one call.
* All manual controls keep `SetCanOptimize(false)` so that optimisation campaigns do not trigger actions
  accidentally.

