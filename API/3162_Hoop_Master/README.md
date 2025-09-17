# Hoop Master Strategy

## Overview

The Hoop Master strategy is a pending breakout system that continuously keeps two stop orders around the current price. The original MetaTrader 5 expert advisor places a buy stop above the market and a sell stop below the market. When one side triggers, the opposite order is cancelled and both sides are re-created with a larger volume. The StockSharp port follows the same idea by managing stop orders and optional martingale sizing inside a single strategy class.

The strategy can also attach protective stop-loss and take-profit orders to any open position. A trailing stop module gradually moves the protective stop when the market advances in the trade direction.

## Trading Logic

1. On every completed candle the strategy recalculates the placement levels for the breakout stops.
2. If no position is open, both a buy stop and a sell stop are registered at a configurable pip distance from the current bid/ask.
3. When either pending stop is filled, the opposite stop is removed. New breakout stops are submitted immediately using double the base volume.
4. After a trade is opened the strategy creates independent stop-loss and take-profit orders. A trailing engine can move the stop towards the price once the move is large enough.
5. When the position is closed, all protective orders are cancelled and the breakout orders are re-initialized with the base volume on the next signal.

## Parameters

| Parameter | Description |
| --- | --- |
| **Candle Type** | Candle data type used for the bar-by-bar logic. |
| **Order Volume** | Base volume for each breakout order. The martingale step uses twice this amount. |
| **Stop Loss (pips)** | Distance in pips between the entry price and the protective stop order. Set to 0 to disable. |
| **Take Profit (pips)** | Distance in pips between the entry price and the protective target order. Set to 0 to disable. |
| **Trailing Stop (pips)** | Distance used when moving the trailing stop. Set to 0 to disable trailing. |
| **Trailing Step (pips)** | Minimal price improvement (in pips) required before the trailing stop is updated. |
| **Indent (pips)** | Offset, measured in pips, added above the ask and below the bid when placing breakout stops. |

## Order Management Details

- The strategy continuously tracks the best bid/ask quotes. When quotes are not available it falls back to the latest trade price or candle close.
- All orders are aligned to the instrument's price step to avoid invalid prices.
- Protective stop and take-profit orders are replaced whenever a new position appears.
- Trailing only runs when both the trailing distance and step parameters are above zero. The stop is moved in the trade direction when the desired improvement is large enough.

## Notes

- Ensure the connected broker or simulator supports stop and limit orders for the selected instrument.
- The martingale step can increase exposure quickly. Adjust the base volume to remain within acceptable risk limits.
- The strategy expects to receive Level1 data (bid/ask) along with candle data so that breakout prices can be calculated accurately.
