# Rubber Bands Grid Strategy

## Overview
- Conversion of the MetaTrader 4 expert advisor **RUBBERBANDS_2.mq4**.
- Runs a symmetric grid around the current price using best bid/ask quotes instead of candles.
- Keeps separate ledgers for long and short exposure so the behaviour matches the hedged MT4 implementation.
- Implements session level profit and loss controls and a manual quiesce/stop mode identical to the original inputs.

## Trading Logic
1. The strategy subscribes to `SubscribeLevel1()` and reacts to every change of the best bid and best ask.
2. Two floating extremes (`_upperExtreme` / `_lowerExtreme`) capture the highest and lowest ask price reached since the last reset. They are initialised from parameters when `UseInitialValues` is true, otherwise the first received ask price is used.
3. When there are no open trades and the server time hits the first tick of a minute (second equals zero) the strategy requests both a market buy and a market sell. This mirrors the MT4 behaviour where buy/sell flags are set every minute while the book is empty.
4. Every time the ask price advances by `GridStepPoints` points above the stored high a new sell order is issued. Every drop by the same distance below the stored low triggers a new buy order. The extremes are updated to the current ask after each trigger so the ladder “stretches” with price.
5. The total number of simultaneously open trades (sum of long and short legs) is limited by `MaxTrades`.
6. Floating profit is calculated from the current bid/ask: long profits use bid minus average long price, short profits use average short price minus ask. The helper `PriceToMoney` converts price differences into account currency using `PriceStep`/`StepPrice` when available.
7. When floating profit reaches `SessionTakeProfitPerLot * OrderVolume` and `UseSessionTakeProfit` is enabled, all exposure is flattened. Likewise floating loss below `-SessionStopLossPerLot * OrderVolume` triggers a full exit when `UseSessionStopLoss` is enabled.
8. Manual flags reproduce the original EA options: `CloseNow` enforces a flat start, `QuiesceMode` keeps the strategy idle while flat, and `StopNow` stops new entries without interfering with existing positions.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Volume for every market order (MT4 `Lots`). |
| `MaxTrades` | Maximum count of simultaneously open trades (MT4 `maxcount`). |
| `GridStepPoints` | Distance in price points between grid layers (MT4 `pipstep`). |
| `QuiesceMode` | If enabled the strategy waits once flat, identical to `quiescenow`. |
| `TriggerImmediateEntries` | Opens an initial buy and sell as soon as the strategy is ready (`donow`). |
| `StopNow` | Pauses automated entries while keeping current positions alive (`stopnow`). |
| `CloseNow` | Requests an immediate flatten on start (`closenow`). |
| `UseSessionTakeProfit` & `SessionTakeProfitPerLot` | Session level floating profit target per lot. |
| `UseSessionStopLoss` & `SessionStopLossPerLot` | Session level floating loss threshold per lot. |
| `UseInitialValues`, `InitialMax`, `InitialMin` | Optional restart support that reuses previous extremes (`useinvalues`, `inmax`, `inmin`). |

## Implementation Notes
- All internal state is tab-indented and stored in fields rather than collections to follow project guidelines.
- Market orders are throttled by tracking `_activeBuyOrder` and `_activeSellOrder` so no duplicate requests are sent while the previous one is pending.
- Hedged accounting is performed in `OnOwnTradeReceived` where long and short average prices/volumes are updated independently and converted to floating profit for stop logic.
- `TryCloseAll()` mirrors the MT4 `close1by1()` routine by submitting opposite market orders until both ledgers are flat and then resetting the extremes to the latest ask.
- The strategy relies exclusively on high level API calls (`SubscribeLevel1()`, `BuyMarket`, `SellMarket`) and avoids direct indicator access as required by the repository rules.
