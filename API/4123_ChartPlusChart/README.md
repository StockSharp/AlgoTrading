# Chart Plus Chart Strategy

## Overview
The **Chart Plus Chart Strategy** is a StockSharp conversion of the MetaTrader 4 expert advisor that ships with two minimal EAs (`Chart1.mq4` and `Chart2.mq4`). The original robots did not execute trades; instead they continuously pushed the latest close price, total order count, account balance, and the profit of the first open order into shared DLL slots so that other charts could read them. The StockSharp version reproduces this data bridge without relying on external DLLs. It listens to one or two configurable candle streams and broadcasts portfolio metrics through strongly typed snapshots.

## Key ideas from the MQL version
- Each MT4 chart instance wrote four numbers into the shared DLL (`Close[0]`, `OrdersTotal()`, `AccountBalance()`, `OrderProfit()`).
- The EAs had no trading logic; their only responsibility was to keep the shared values fresh at every tick and on initialization.
- Two instances ran simultaneously on different charts, each using a different memory offset (10 and 70) to avoid collisions.

## StockSharp implementation
- High-level candle subscriptions (`SubscribeCandles`) collect close prices. The primary stream is mandatory while the secondary stream can be toggled with `UseSecondaryStream`.
- Every time a finished candle is received the strategy composes a `ChartSnapshot` that contains the candle close and live portfolio metrics (active order count, `Portfolio.CurrentValue`, `Portfolio.CurrentProfit`).
- Snapshots are stored internally (`PrimarySnapshot`, `SecondarySnapshot`) and published through the `SnapshotUpdated` event so that UI widgets, dashboards or other strategies can consume the data.
- Order, trade and position callbacks (`OnOrderRegistered`, `OnOrderChanged`, `OnNewMyTrade`, `OnPositionChanged`) refresh the stored snapshots, mirroring the constant updates performed by the MT4 scripts even when no new candle is printed.

## Event-driven workflow
1. The strategy starts and subscribes to the selected candle types.
2. Once the first candle closes, the corresponding snapshot is initialized and the `SnapshotUpdated` event is fired.
3. Later updates (either from new candles or from order/portfolio changes) mutate the existing snapshot using C# `record struct` `with` expressions, ensuring consumers always get the latest portfolio figures.
4. Disabling the secondary stream stops the extra subscription while the primary stream continues to operate normally.

## Snapshot structure
```csharp
public record struct ChartSnapshot
{
        public decimal LastClose { get; init; }
        public int ActiveOrders { get; init; }
        public decimal AccountBalance { get; init; }
        public decimal TotalProfit { get; init; }
}
```
- **LastClose** mirrors `Close[0]` from MetaTrader.
- **ActiveOrders** corresponds to the live order count (`ActiveOrders.Count`).
- **AccountBalance** is taken from `Portfolio.CurrentValue` (falls back to `0` if the adapter has not provided a value yet).
- **TotalProfit** reuses `Portfolio.CurrentProfit`, giving the aggregate floating profit instead of the single-order value from MT4.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `PrimaryCandleType` | Timeframe or candle data type tracked by the first stream. |
| `SecondaryCandleType` | Optional second candle data type. Only used when `UseSecondaryStream` is enabled. |
| `UseSecondaryStream` | Enables or disables the secondary candle subscription and snapshot. |

All parameters expose StockSharp metadata via `SetDisplay`, making them visible and editable in Designer, Shell, or Runner.

## Consuming the data
```csharp
var strategy = new ChartPlusChartStrategy
{
        PrimaryCandleType = TimeSpan.FromMinutes(15).TimeFrame(),
        SecondaryCandleType = TimeSpan.FromHours(1).TimeFrame(),
        UseSecondaryStream = true
};

strategy.SnapshotUpdated += (stream, snapshot) =>
{
        Console.WriteLine($"[{stream}] close={snapshot.LastClose:0.#####} orders={snapshot.ActiveOrders} " +
                $"balance={snapshot.AccountBalance:0.##} profit={snapshot.TotalProfit:0.##}");
};
```
- Subscribe before starting the strategy to capture the very first update.
- The latest values can also be pulled on demand through `strategy.PrimarySnapshot`, `strategy.SecondarySnapshot`, or `strategy.GetSnapshot(stream)`.

## Differences vs. MetaTrader
- Instead of writing into shared DLL memory slots, the StockSharp version broadcasts strongly typed events, making integration with C# UIs or dashboards straightforward.
- `Portfolio.CurrentProfit` replaces `OrderProfit()` because StockSharp tracks aggregated strategy profit rather than the first ticket only. This provides a more robust overview but may differ from the MT4 value when multiple orders are open.
- Candle processing waits for finished bars (`CandleStates.Finished`) to avoid partial updates. In MT4 the scripts read the live tick close.
- The secondary stream can be disabled entirely, allowing the bridge to run from a single chart if desired.

## Usage tips
- Attach the strategy to a connector or testing environment that updates `Portfolio.CurrentValue` and `Portfolio.CurrentProfit` so the snapshots can reflect the live account.
- For dashboards, combine the emitted snapshots with UI bindings (WPF, WinForms, Blazor) to display synchronized metrics across multiple views.
- Because no trading commands are issued, the strategy can run alongside other trading algorithms without interfering with their order flow.
