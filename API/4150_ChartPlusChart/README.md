# Chart Plus Chart Strategy

## Overview
Chart Plus Chart is a utility strategy converted from the MetaTrader scripts *Chart1.mq4* and *Chart2.mq4*. The original code uses
a DLL named `SharedVarsDLLv2` to synchronise simple account metrics between multiple charts: every tick it writes the latest close
price, the number of open orders, the account balance and the floating profit of the first position into shared memory slots. Other
scripts can then read those slots to display aggregated information on custom panels.

The StockSharp port keeps the data-sharing spirit without relying on external binaries. It exposes a static
`ChartPlusChartSharedStorage` helper that mimics the DLL interface: strategies can publish values under a configurable base index
and other components can read them through thread-safe getters. The provided `ChartPlusChartStrategy` does not place any trades; it
only monitors the assigned security and portfolio, feeding the shared storage with up-to-date statistics.

## Published metrics
1. Subscribe to the candle series defined by `CandleType` and wait until each candle finishes so the close price is final.
2. Calculate the number of active orders registered by the strategy, ignoring any order already completed, cancelled or failed.
3. Retrieve the most recent portfolio value (`Portfolio.CurrentValue`, or `Portfolio.BeginValue` as a fallback).
4. If the strategy currently holds a position, compute its floating profit using the latest price and the average entry price.
5. Write all four values to the shared storage:
   - `SetFloat(baseIndex, closePrice)`
   - `SetInt(baseIndex, activeOrders)`
   - `SetFloat(baseIndex + 1, accountValue)`
   - `SetFloat(baseIndex + 2, floatingPnL)`
6. On every executed trade the routine is invoked again so intrabar fills are also reflected in the shared variables.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Primary timeframe processed before publishing data. |
| `ChannelIndex` | `int` | `10` | Base slot used to store information inside `ChartPlusChartSharedStorage`. |

## Differences from the original MetaTrader scripts
- No external DLL is required. All shared-memory behaviour is reproduced via the in-process `ChartPlusChartSharedStorage`
  dictionary.
- MetaTrader updates the values on every incoming tick. The StockSharp implementation publishes data for completed candles and
  after every own trade, which prevents noisy intermediate prices while still reacting promptly to executions.
- The MQL scripts call `OrderProfit()` on the first order returned by `OrderSelect`. The StockSharp version computes floating PnL
  from the net position so the reported value matches the platform’s netting model.
- When the strategy stops, the shared slots are cleared so other tools know the data is no longer maintained.

## Usage tips
- Run `ChartPlusChartStrategy` on the account you want to monitor and set matching `ChannelIndex` values for producers and
  consumers so they read the same slots.
- Consume the shared data by calling `ChartPlusChartSharedStorage.TryGetFloat`/`TryGetInt` from visual dashboards or companion
  strategies.
- Because no orders are created automatically, the strategy is safe to run alongside discretionary or automated trading systems as
  a lightweight telemetry publisher.
