# Magic Number Wise EA Profit/Loss Dashboard Strategy (C#)

This folder contains the StockSharp conversion of the MetaTrader 5 utility **Magic-Number-wise-EA-Profit-Loss-Live-Dashboard_v1**. The original expert advisor scans the trading history, groups deals by magic number and shows a real-time table on the chart. The StockSharp port keeps the periodic aggregation logic and exposes the same information through the strategy log so it can be monitored in the Designer or any other StockSharp host.

## What the strategy does

* Collects every order and trade that passes through the strategy portfolio.
* Builds an in-memory table grouped by an identifier:
  * By default the table key is `Order.UserOrderId` (the usual place where MT bots store the magic number).
  * Optionally the order comment can be used as the identifier to mimic brokers that encode EAs inside the comment string.
* Tracks, per identifier:
  * Number of executed deals.
  * Sum of realised PnL reported by StockSharp for each `MyTrade`.
  * Last known symbol (instrument id) taken from either the trade or the originating order.
  * Optional floating PnL taken from the current portfolio position of that symbol.
* Prints a formatted snapshot on a fixed timer. The output mirrors the MT5 table header: `Magic Id | Deals | Closed P/L | Floating P/L | Symbol | Comment`.

Because StockSharp works with net positions per security, the floating PnL is assigned by symbol. This behaves like the original script as long as a single expert controls one symbol per magic number, which is how MetaTrader hedging accounts usually operate.

## Parameters

All parameters are created through `Param()` so they can be edited or optimised inside Designer.

| Parameter | Description |
|-----------|-------------|
| `RefreshInterval` | Timer interval used to refresh the dashboard snapshot (default 5 seconds). |
| `GroupByComment` | When `true`, group entries by `Order.Comment` instead of `Order.UserOrderId`. Useful for brokers that do not forward user ids. |
| `IncludeOpenPositions` | When enabled, attaches floating PnL from `Portfolio.Positions` to the summary lines. |

## How it works

1. When the strategy starts it validates the refresh interval and creates a `System.Threading.Timer` that immediately triggers the first report.
2. `OnOrderRegistered` and `OnOrderChanged` ensure that new identifiers are registered as soon as orders appear and capture their symbol/comment metadata.
3. `OnNewMyTrade` increments the deal count and aggregates the realised PnL returned by StockSharp for each trade.
4. Every timer tick the strategy:
   * Resets floating PnL entries and, if enabled, synchronises them with the latest `Portfolio.Positions` snapshot.
   * Copies the collected data into immutable snapshots, sorts them by identifier and prints the formatted table to the log.

The approach mirrors the MT5 dashboard, except that instead of drawing labels on the chart the port relies on log output. This keeps the behaviour deterministic and allows the same code to run in both backtesting and live environments.

## Usage tips

1. Attach the strategy to the account you want to monitor (no market data subscription is required).
2. If your MetaTrader bridge writes the EA identifier to the order comment, set `GroupByComment = true`.
3. Keep `IncludeOpenPositions` enabled to see floating PnL next to the realised column; disable it if the adapter does not populate `Position.PnL` yet.
4. Review the log window: each refresh produces an aligned table identical to the MT5 panel.

## Files

* `CS/MagicNumberWiseEaProfitLossDashboardStrategy.cs` – implementation of the dashboard logic.
* `README.md` – English documentation.
* `README_cn.md` – Simplified Chinese overview.
* `README_ru.md` – Russian overview.
