# BadOrders Strategy

## Overview
The **BadOrders Strategy** is a direct port of the MetaTrader 4 expert advisor `BadOrders.mq4`. The original script was intentionally written to demonstrate how incorrect order management leads to rejected trades. On every incoming tick it:

1. Forcefully closes the most recently opened position at the current bid price.
2. Places a new buy stop 100 points above the bid.
3. Immediately modifies that pending order to sit 100 points *below* the bid, violating broker distance rules and provoking an error.

The StockSharp version reproduces this behaviour with the high-level API. It subscribes to Level 1 quotes to monitor the best bid and replays the same close–place–invalidate cycle whenever a quote arrives.

## Implementation details
- **Data stream**: `SubscribeLevel1()` is used because the MT4 script reacts to every tick rather than candle completions.
- **Order management**: Open positions are closed with the `ClosePosition()` helper. Pending stops are managed through `BuyStop()` and `ReRegisterOrder()` so we can immediately move the stop order to an illegal price, mimicking the broken workflow of the source code.
- **Price normalisation**: All prices are normalised via `Security.ShrinkPrice()` and the MetaTrader concept of `Point` is emulated through the instrument `PriceStep`. When no tick size is available the strategy falls back to `0.0001`.
- **Protective logic**: Before calling `ClosePosition()` the code checks for existing liquidation orders to avoid stacking duplicate exit requests.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `DistancePoints` | Distance in MetaTrader “points” added above and below the current bid when placing or re-registering the stop order. | `100` |

## Behaviour summary
- Whenever the bid changes the strategy attempts to flatten any open position.
- A buy stop is submitted at `bid + DistancePoints * PointValue` after the position is closed.
- The same order is immediately re-registered to `bid - DistancePoints * PointValue`, which violates exchange rules and is expected to fail — precisely mirroring the intentional mistakes in `BadOrders.mq4`.

> **Note**: This project exists purely for parity with the MT4 sample and is not intended for live trading.
