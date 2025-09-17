# Simple Profit By Periods Panel 2 Extended Strategy

## Overview
The original MetaTrader expert advisor only monitored trading statistics and drew a floating panel with daily, weekly, and monthly profit. This C# port reproduces that behaviour for StockSharp strategies. It subscribes to a lightweight candle stream that acts as a periodic timer, aggregates realized trade results, and publishes the metrics through the strategy `Comment` field so they are always visible in the UI.

Unlike the MetaTrader script, the StockSharp version has no graphical labels. Instead, the comment contains six lines:

1. Daily realized profit and the percentage change versus the balance at the start of the day.
2. Weekly realized profit with the same percentage calculation.
3. Monthly realized profit and percentage gain or loss.
4. Daily deal counter (number of filled trades).
5. Weekly deal counter.
6. Monthly deal counter.

The panel logic is purely informational. No orders are sent and the current position is never altered.

## Features
- **Realized PnL tracking.** Each `MyTrade` updates the internal history using the delta of `PnLManager.RealizedPnL` (or the strategy `PnL` if the manager is unavailable).
- **Trading-day adjustments.** When the calendar date falls on Saturday or Sunday, the reference “daily” period shifts back to the last trading day (Friday), mirroring the MetaTrader implementation.
- **Weekly and monthly ranges.** The start of the week is calculated from the adjusted trading day (Monday through Friday). Monthly profit resets on the first day of the current month.
- **Comment-based dashboard.** The aggregated values are exposed via `Strategy.Comment`, so the information is visible in the StockSharp UI, exported logs, or any monitoring scripts.
- **Automatic pruning.** Old trade entries are discarded once they are more than one week older than the active reporting window, preventing unbounded memory use during long sessions.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Candle series used to trigger periodic refreshes. Select a timeframe that matches how often you want the comment to be updated. | `1 minute time frame` |

## How it works
1. **Start-up.** `OnStarted` initialises the realized PnL baseline, subscribes to the configured candle series, and immediately refreshes the dashboard.
2. **Trade accounting.** For every `MyTrade`, the strategy verifies that the fill belongs to the configured security, computes the realized delta, stores the timestamp and profit, and refreshes the dashboard.
3. **Periodic refresh.** Each completed candle (acting as a timer) recomputes the day/week/month sums in case the balance changed externally or the week rolled over.
4. **Percentage calculation.** Percent values are derived from the current portfolio balance versus the balance prior to the accumulated profit (`previous = current - periodProfit`). If the previous balance is zero or negative the percentage is reported as `0.00%` to avoid division-by-zero artefacts.

## Usage tips
- The strategy requires an attached portfolio so the balance (`Portfolio.CurrentValue`) can be read. Without a portfolio the comment will show zero balances and zero percentages.
- Choose a candle series that is always available for the instrument (for example, the native trading timeframe). The strategy does not rely on candle prices; it only needs the periodic “tick” to refresh the comment.
- Because only realized PnL is tracked, open positions will not affect the reported profit until they are closed.
- The comment uses the portfolio currency if available, otherwise it falls back to the security currency. If neither value exists, the amount is shown without a prefix.

## File structure
- `CS/SimpleProfitByPeriodsPanel2ExtendedStrategy.cs` – strategy implementation.
- `README.md` – this documentation.
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.

## Differences from MetaTrader version
- The MetaTrader script drew graphical labels; the StockSharp port uses the comment field to stay lightweight and avoid manual chart drawing.
- The StockSharp environment exposes realized PnL through `PnLManager`, so there is no need to read account history manually.
- Trade filtering relies on the strategy security rather than magic numbers. Attach the strategy to the specific instrument you want to monitor.

## Limitations
- Only realised results are supported; floating profit or swap on open positions is ignored (matching the original behaviour).
- The strategy assumes that `Portfolio.CurrentValue` is updated promptly by the connection adapter. If the value is stale the percentage numbers can lag until the next balance update.
