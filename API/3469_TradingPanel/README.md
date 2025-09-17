# TradingPanelStrategy

## Overview
`TradingPanelStrategy` is a StockSharp port of the MetaTrader 4 expert advisor **EA_TradingPanel**. The original script exposed a manual panel where the trader configured the number of simultaneous trades, lot size and protective distances before pressing **BUY** or **SELL**. In the StockSharp version the same behaviour is automated: once the operator sets the `Direction` parameter the strategy fires a batch of market orders on the next finished candle and instantly resets the direction back to `None`.

The logic is intentionally simple so that the module can be combined with external signals or manual supervision. All orders inherit optional stop-loss and take-profit distances measured in pips, mirroring the risk controls available in the MQL implementation.

## Workflow
1. When the strategy starts it calculates the pip size from `Security.PriceStep`. For 1/3/5-digit Forex symbols the value is multiplied by ten, matching the MetaTrader conversion between points and pips.
2. If stop-loss or take-profit offsets are non-zero the strategy enables `StartProtection` to manage exits with market orders.
3. The strategy subscribes to the candle series specified by `CandleType`. After each finished candle it checks the `Direction` parameter.
4. If a direction is requested and the engine allows trading, the strategy sends `NumberOfOrders` market orders using `OrderVolume` for each ticket.
5. After the batch is dispatched the strategy logs the action and automatically sets `Direction` back to `None`, ready for the next manual trigger.

This design keeps the module stateless between executions. Traders can repeatedly set `Direction` to `Buy` or `Sell` whenever they require a new batch of orders; the execution always happens on the next completed candle to avoid acting on partially formed market data.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `NumberOfOrders` | `int` | `1` | Number of market orders sent in the next batch. |
| `OrderVolume` | `decimal` | `0.01` | Volume applied to each market order. |
| `StopLossPips` | `decimal` | `2` | Stop-loss distance converted from pips to absolute price using the current instrument metadata. Set to `0` to disable. |
| `TakeProfitPips` | `decimal` | `10` | Take-profit distance in pips. Set to `0` to disable. |
| `Direction` | `TradeDirection` | `None` | Requested direction for the next execution. The strategy resets the value after the orders are placed. |
| `CandleType` | `DataType` | `TimeFrameCandle(1m)` | Candle series used to trigger execution. |

## Notes
- The strategy requires a valid `Security` with properly configured `PriceStep` (and optionally `Decimals`). Without this metadata pip calculations fall back to `1`.
- `StartProtection` uses market orders for exits to mimic how the MQL panel closed positions at stop-loss or take-profit levels.
- Because execution happens on finished candles, traders can synchronise order batches with custom analytics or external signals by updating `Direction` before the candle closes.
