# Get Last Nth Open Trade Strategy

## Overview
The **Get Last Nth Open Trade Strategy** mirrors the MetaTrader expert by scanning the current portfolio and publishing the details of the N-th most recent open position. The strategy is informational only: it never places or cancels orders, but periodically inspects the broker positions and reports a formatted snapshot to the strategy log so the operator can verify ticket, direction, quantity, price and profit without leaving the Designer.

## Key Features
- Periodic timer that refreshes the snapshot at the configured interval, with the very first report executed immediately after the strategy starts.
- Optional filters matching the original expert: restrict the scan to the strategy security or only to positions that carry the provided strategy identifier (the analogue of the MetaTrader magic number).
- Positions are sorted by their last change time in descending order so index `0` represents the most recent trade, fully reproducing the "last from the end" behaviour of the source script.
- Human-readable report including the position identifier, symbol, side, quantity, average price, profit, last change timestamp and, when available, the originating strategy identifier.
- Thread-safe timer handling that prevents overlapping executions when a previous snapshot is still being processed.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `EnableMagicNumber` | When enabled, only positions whose strategy identifier matches `MagicNumber` are reported. | `false` |
| `EnableSymbolFilter` | When enabled, the strategy scans only the positions associated with `Strategy.Security`. | `false` |
| `MagicNumber` | Textual strategy identifier (MetaTrader magic number analogue) that positions must match when the magic-number filter is active. | `"1234"` |
| `TradeIndex` | Zero-based index of the position to report after sorting by last change time (index `0` = newest). | `0` |
| `RefreshInterval` | Time span between successive snapshots. The first execution runs immediately on start. | `1` second |

## How It Works
1. During `OnStarted` the strategy validates that a portfolio is attached and that `RefreshInterval` is positive.
2. A `System.Threading.Timer` starts with zero delay so the very first snapshot is generated instantly. The timer then repeats according to `RefreshInterval`.
3. Each callback iterates through `Portfolio.Positions`, skipping entries with zero quantity and applying the optional symbol and magic-number filters.
4. The remaining positions are sorted by `LastChangeTime` descending. If the requested `TradeIndex` is outside the available range the strategy logs an explanatory message and waits for the next tick.
5. When a position is available, a formatted block containing ticket, symbol, side, quantity, average price, profit, last change time and optional strategy identifier is logged via `LogInfo`.
6. All processing is wrapped in a lock-free guard implemented with `Interlocked.Exchange` so that heavy I/O or slow logging never produces overlapping timer executions.

## Usage Notes
- Attach the strategy to a connector and assign the desired `Portfolio` and, if you plan to use the symbol filter, also set `Strategy.Security`.
- Populate `MagicNumber` with the exact string used as `StrategyId` by other strategies if you need to replicate the MetaTrader magic-number filter.
- The report is written to the strategy log (Designer output, Runner log, etc.). If you require UI display, bind a log listener to show the message wherever needed.
- `TradeIndex` counts from zero. Setting it to `0` retrieves the latest trade; `1` retrieves the previous one, and so forth.

## Differences Compared to the MQL Version
- MetaTrader exposes stop-loss, take-profit and comment fields per position; StockSharp positions do not offer direct access to those attributes, so the report focuses on identifier, direction, quantity, prices and profit metrics.
- Instead of updating the chart comment, this port uses the StockSharp logging infrastructure via `LogInfo`.
- The StockSharp version uses the position `LastChangeTime` for ordering, which reflects the most recent update from the trading connection. MetaTrader sorts by the numeric ticket identifier; both approaches ensure the newest trade appears at index `0`.
- Magic-number filtering uses the textual `StrategyId` embedded in positions. Leave `EnableMagicNumber` disabled if positions do not carry such metadata.
