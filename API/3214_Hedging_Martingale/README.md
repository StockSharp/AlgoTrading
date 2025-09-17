# Hedging Martingale Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor "Hedging Martingale" (folder `MQL/23693`). It keeps a balanced hedge by opening both a long and a short position on every new bar and then applies a martingale averaging scheme. When price moves adversely by a configurable pip distance, the strategy adds a new position on the losing side with an increased volume while keeping the opposite hedge in place. Floating profit is managed using money- and percent-based targets together with an optional trailing lock.

## Trading Logic
- **Initial hedge**: whenever the strategy is flat and a new candle closes, it simultaneously buys and sells using the same base volume.
- **Martingale steps**: if price moves against one side by `Pip Step` pips, an additional order is opened on that side. The volume is multiplied by `Volume Multiplier`, emulating the progressive lot sizing from the MQL version. The opposite side remains open to maintain the hedge.
- **Per-trade take profit**: every open entry has an individual take-profit distance defined by `Take Profit (pips)`. When the market moves in favor of a leg by that distance, the leg is reduced by issuing an offsetting order.
- **Basket exits**: the entire set of positions can be closed when floating profit reaches a money target, a percentage of the starting equity, or after a trailing lock gives back more than the allowed retracement. These behaviours replicate `Take_Profit_In_Money`, `Take_Profit_In_percent`, and `TRAIL_PROFIT_IN_MONEY2` from the original expert.
- **Trade limits**: the `Max Trades` parameter restricts how many martingale steps can be active. If `Close On Max` is enabled, the basket is liquidated once the limit is exceeded.

## Parameters
| Name | Description |
| ---- | ----------- |
| Candle Type | Timeframe that drives the logic. Each finished candle can trigger new hedging actions. |
| Use Money TP / Money Take Profit | Enable and define the floating profit (in currency units) that closes all positions. |
| Use Percent TP / Percent Take Profit | Close the basket when the floating profit reaches a percentage of the starting portfolio value. |
| Enable Trailing / Trailing Start / Trailing Step | Activate the money-based trailing lock for the basket and configure the trigger level together with the permitted profit give-back. |
| Take Profit (pips) | Distance in pips for per-leg take-profit exits. |
| Pip Step | Adverse price movement (in pips) required before adding another martingale order. |
| Base Volume | Initial volume for both the buy and sell legs. |
| Volume Multiplier | Multiplier applied to the largest position volume when adding martingale entries. |
| Max Trades | Maximum number of simultaneously open entries (across both directions). |
| Close On Max | Whether to liquidate all positions once the maximum trade count is breached. |

## Notes
- The strategy uses `BuyMarket` and `SellMarket` for all order placements, mirroring the market execution model of the source expert.
- Volume values are normalized to the instrument's lot step to avoid rejected orders.
- When the strategy becomes flat, the trailing lock is reset so that new baskets start with a clean profit reference.

## Files
- `CS/HedgingMartingaleStrategy.cs` – implementation of the converted strategy (C#).
- `README.md` – this documentation (English).
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.
