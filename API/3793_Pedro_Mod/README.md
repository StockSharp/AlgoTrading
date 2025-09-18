# Pedro Mod Strategy

## Overview

This strategy is a StockSharp port of the **Pedroxxmod** MetaTrader 4 expert advisor. The original EA waits for the market to move a
few pips away from a reference price and then opens a contrarian position. Subsequent orders are averaged in the same direction
whenever the price retraces by a configurable distance. The StockSharp implementation keeps the behaviour intact while exposing
strongly typed parameters through the high-level `Strategy` API.

## Trading logic

1. Subscribe to Level1 best bid/ask quotes and cache the most recent values.
2. When no trades are open, store the current ask price as the reference entry level. Trading is only allowed between
   `StartHour` and `EndHour`, and from `StartYear` onward.
3. If the best ask rises by `Gap` MetaTrader pips above the reference, submit a market sell order. If it drops by `Gap` pips,
   submit a market buy order. Protective stop-loss and take-profit levels are attached automatically by calling
   `SetStopLoss` / `SetTakeProfit` with the same pip distances as the expert advisor.
4. Once a basket direction is established, the strategy keeps a FIFO list of the synthetic positions to emulate the hedging
   style of MetaTrader. As long as the current basket size is below `MaxTrades`, averaging orders are added when the best ask
   returns within `ReEntryGap` pips of the latest entry price.
5. Money management can either use the fixed `Lots` parameter or dynamically allocate volume according to the EA rule
   `floor(Equity / 20000)`, capped by `MaxLots`. All volumes are normalized against the security's volume step/min/max.
6. Out-of-hours updates reset the internal entry anchors to avoid spurious trades when the next session starts.

## Parameters

| Name | Description |
|------|-------------|
| `Lots` | Fixed order volume when money management is disabled. |
| `StopLoss` | Protective stop distance in MetaTrader pips. Set to `0` to disable the stop. |
| `TakeProfit` | Profit target distance in MetaTrader pips. Set to `0` to disable the target. |
| `Gap` | Distance in MetaTrader pips the ask must move away from the reference before opening the first trade. |
| `MaxTrades` | Maximum number of simultaneously open trades (basket size). |
| `ReEntryGap` | Distance in MetaTrader pips that triggers averaging orders in the basket direction. |
| `MoneyManagement` | Enables the dynamic volume rule `floor(Equity / 20000)` when set to `true`. |
| `MaxLots` | Upper bound for the dynamically calculated volume. |
| `StartHour` / `EndHour` | Trading window in exchange server time (inclusive). |
| `StartYear` | Calendar year from which trading is permitted. Earlier data is ignored. |

## Notes

- The strategy only consumes Level1 data and does not request candles. It is therefore lightweight and reacts immediately to
  quote changes, just like the MT4 `start()` tick handler.
- Stops and targets rely on the helper methods from `Strategy` to translate MetaTrader pip distances into broker-specific
  price levels. Ensure the connected venue exposes correct `PriceStep`, `StepPrice`, and `VolumeStep` values.
- The synthetic basket counter allows the strategy to mimic hedging accounts even though StockSharp aggregates the position.
  Partial fills and stop hits are handled via the `OnPositionChanged` callback that maintains the FIFO queues.
- Python implementation is intentionally omitted according to the repository guidelines.
