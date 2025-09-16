# HistTraining Strategy

## Overview
- Recreates the MetaTrader 4 expert advisor `HistoryTrain.mq4` that waits for external training signals.
- Instead of reading global integers 97, 98 and 99, the C# version exposes boolean parameters (`BuyTrigger`, `SellTrigger`, `CloseTrigger`).
- A lightweight one-minute candle subscription is used only as a timing heartbeat so the triggers are checked on each completed bar.
- The strategy does not calculate indicators or price filters; every trade is initiated by an external workflow that flips the triggers.

## Parameters
| Name | Description |
| --- | --- |
| `OrderVolume` | Trade size submitted with each market order. Defaults to `0.1`, mirroring the fixed lot used in the MQL code. |
| `BuyTrigger` | When set to `true` while the strategy is flat, a market buy order is sent and the flag is reset to `false`. |
| `SellTrigger` | When set to `true` while the strategy is flat, a market sell order is sent and the flag is reset to `false`. |
| `CloseTrigger` | When set to `true` while a position exists, the position is flattened with `ClosePosition()` and the flag returns to `false`. |
| `CandleType` | Candle series that drives the polling loop. The default is a one-minute time frame because the expert only needs a periodic tick. |

## Trading Logic
1. During `OnStarted` the strategy subscribes to the configured candle series and activates `StartProtection()` so existing positions are handled safely.
2. On every finished candle (`CandleStates.Finished`):
   - If `BuyTrigger == true` and `Position == 0`, a market buy order with `OrderVolume` lots is submitted and the trigger is cleared.
   - If `SellTrigger == true` and `Position == 0`, a market sell order with `OrderVolume` lots is submitted and the trigger is cleared.
   - If `CloseTrigger == true` and `Position != 0`, `ClosePosition()` is called to flatten the exposure and the trigger is cleared.
3. The order of evaluation (buy, sell, close) matches the original EA: a close request can immediately flatten a position opened in the same cycle if both triggers are raised simultaneously.

## Manual Signal Workflow
- The legacy MQL expert relied on global variables (`SetInt@8`/`GetInt@4`) written by a separate training tool. The StockSharp port keeps the same idea via explicit parameters.
- An external application, UI button, script or optimization harness can flip the boolean parameters to issue commands. They remain `true` until the associated action succeeds, ensuring the next heartbeat re-evaluates the request if trading was not possible (for example, because a position was already open).
- Because the strategy has no price-based filters, risk management (stop-loss, take-profit, timeouts) must be implemented outside or through additional automation rules if needed.

## Conversion Notes
- Market orders are sent with `BuyMarket`/`SellMarket` exactly once per trigger, replicating `OrderSend` with the hard-coded `0.1` lot size from MetaTrader.
- Position exits use `ClosePosition()` instead of duplicating ticket-handling logic. The effect is identical to the two `OrderClose` branches in the source.
- The polling heartbeat relies on `SubscribeCandles(CandleType)`; no indicators are registered in `Strategy.Indicators`, following the repository guidelines.
- Parameters are grouped under "Manual signals" to highlight that they are not intended for optimizer sweeps (`SetCanOptimize(false)`).
- Comments in the code explain how each trigger maps to the original global variable flow so future maintainers can reconnect external tooling quickly.

## Differences from the MQL Version
- MetaTrader global variables are replaced by user-facing `StrategyParam<bool>` values.
- StockSharp automatically handles order tickets and portfolio synchronization, so there is no explicit `OrderSelect` call.
- The port uses candles as a scheduling mechanism; MetaTrader executed the logic on each tick via `start()`.
- Volume can be adjusted at runtime through the `OrderVolume` property, while the original EA used a fixed constant.

## Additional Remarks
- There is intentionally no Python translation for this strategy at the moment, as requested.
- Tests were not modified; the logic remains entirely within the new strategy folder.
- To integrate with a teaching or replay tool, toggle the parameters via the StockSharp GUI or automation API right before the next candle close to emulate the training flow.
