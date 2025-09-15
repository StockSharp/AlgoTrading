# CM Fishing Strategy

## Overview

The **CM Fishing Strategy** is a grid trading approach adapted from the original MQL script `cm_fishing.mq4`. The strategy opens market orders whenever the price moves a fixed number of points from the last executed trade. It can build a grid of long or short positions and close them when a specified profit target is reached.

This implementation focuses on the core trading logic without the graphical interface of the original script. Orders are executed using StockSharp's high‑level API.

## Parameters

| Name | Description |
|------|-------------|
| `Buy` | Enable or disable opening long positions. |
| `Sell` | Enable or disable opening short positions. |
| `StepBuy` | Price step in points that must be passed downward before a new long position is opened. |
| `StepSell` | Price step in points that must be passed upward before a new short position is opened. |
| `CloseProfitBuy` | Profit threshold for closing all long positions. |
| `CloseProfitSell` | Profit threshold for closing all short positions. |
| `CloseProfit` | Profit threshold that closes any open position regardless of direction. |
| `BuyVolume` | Order volume for each long trade. |
| `SellVolume` | Order volume for each short trade. |

## Trading Logic

1. Track trade prices in real time.
2. When the price falls by `StepBuy` from the last trade level and `Buy` is enabled, send a market buy order.
3. When the price rises by `StepSell` from the last trade level and `Sell` is enabled, send a market sell order.
4. Maintain the average entry price of the current position.
5. Close positions when the unrealized profit exceeds the corresponding `CloseProfit*` parameter.

The strategy works with tick data and is suitable for demonstration and educational purposes.

## Notes

- The implementation does not reproduce the user interface of the original script.
- Only one net position (long or short) is maintained at any time.

