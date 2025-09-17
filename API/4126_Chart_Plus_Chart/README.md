# Chart Plus Chart Monitor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
This strategy is a StockSharp conversion of the MetaTrader helper *ChartPlusChartV2* (files `Chart1.mq4` and `Chart2.mq4`). The original Expert Advisor does not trade by itself. Instead, it gathers a handful of account statistics (last close price, number of open orders, account balance, profit of the first selected order) and writes them into a shared DLL so that several charts can display the same data side by side. The StockSharp version follows the same idea: every market update produces a consolidated snapshot of trading statistics that can be shown on custom dashboards, additional chart areas, or external panels.

Unlike the MQL implementation that relies on `SharedVarsDLLv2.dll`, the C# port leverages the native capabilities of the `Strategy` base class. Snapshots are exposed through read-only properties and can optionally be written to the log for diagnostics. This makes the helper cross-platform and independent from Windows-only DLLs.

## Key Features
- **Centralised account telemetry.** The strategy keeps the latest close price, active order count, portfolio value and last trade profit readily available for other modules or UI bindings.
- **Event-driven updates.** Snapshots refresh on every finished candle, order status change and trade execution, ensuring that the numbers mirror what the MQL scripts published on each tick.
- **Optional verbose logging.** An optional flag reproduces the behaviour of broadcasting the metrics to an external consumer, allowing operators to tail the strategy log instead of reading shared memory.
- **Safe portfolio fallback.** When the connected portfolio does not report a current value yet, the helper falls back to the starting balance, preventing `0` from masking the real account size.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Timeframe used to produce finished candles. Each closed candle triggers a snapshot refresh and provides the closing price that mirrors `Close[0]` from the MQL code. |
| `LogOnEveryUpdate` | `false` | When enabled the helper writes every snapshot to the strategy log, emulating the shared DLL broadcast with human-readable messages. Keep it disabled in production to avoid excessive logging. |

## Output Properties
- `LastClosePrice` – final price of the most recent finished candle processed by the strategy.
- `OpenOrdersCount` – number of active orders returned by `ActiveOrders.Count`, analogous to `OrdersTotal()`.
- `AccountValue` – portfolio current value or starting balance if the current value is not supplied yet.
- `LastTradeProfit` – realised PnL difference produced by the latest trade compared to the previous total PnL.
- `LastSnapshotTime` – timestamp of the last update. Useful for binding to dashboards and detecting stale information.

## Execution Flow
1. **Start-up.** On `OnStarted` the helper subscribes to candles of the configured timeframe, resets the PnL baseline and publishes an initial snapshot so that consumers immediately receive account information.
2. **Candle updates.** Every finished candle updates the stored close price and refreshes all statistics. This mirrors the behaviour of the MQL scripts, which called `SetFloat` / `SetInt` on every tick.
3. **Order state changes.** `OnOrderChanged` keeps the order counter in sync with the trading engine, even if no candle closed yet.
4. **Trade executions.** `OnNewMyTrade` recomputes the realised PnL delta to populate `LastTradeProfit`, allowing dashboards to highlight the latest fill.
5. **Optional logging.** When `LogOnEveryUpdate` is enabled, each refresh writes a detailed line such as `Snapshot 2024-03-25T10:15:00Z: close=1.1234; orders=2; account=10025.50; last trade=15.20; total pnl=120.85.`

## Integration Tips
- Bind the output properties to WPF/WinForms dashboards via data binding or to chart overlays to recreate the “chart plus chart” layout without DLLs.
- Pair the helper with other trading strategies by running it on the same connector and portfolio; it does not submit orders on its own, so it safely coexists with live systems.
- Adjust `CandleType` to match the refresh cadence of your UI. A lower timeframe provides more frequent updates at the cost of extra candle traffic.
- Use the logging option temporarily when validating automation pipelines or when exporting telemetry to text-based monitoring tools.

## Conversion Notes
- The shared memory indices (`IND`, `IND+1`, `IND+2`) used in the MQL scripts are replaced with strongly-typed properties, eliminating manual offset management.
- Account profit is calculated from the strategy PnL difference to provide the same “last order profit” information without iterating through all trades.
- No trading logic was added during the conversion, staying faithful to the auxiliary nature of the original helper.

