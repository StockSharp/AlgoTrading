# CM Panel Strategy

## Overview
The **CM Panel Strategy** is a manual pending-order helper that recreates the behaviour of the original MetaTrader 5 script "cm panel". Instead of drawing on-screen controls, the StockSharp port exposes interactive parameters that work like buttons: setting a flag to `true` places or cancels pending stop orders and the flag immediately resets to `false`, mimicking the push-button workflow of the panel. The strategy keeps separate configuration for buy and sell orders, including distances, volumes, and protective targets expressed in points.

The conversion relies entirely on StockSharp’s high-level API. Pending orders are submitted with the `BuyStop` and `SellStop` helpers, while post-fill protection is implemented by registering independent stop-loss and take-profit orders. Price and volume values are automatically adapted to the security’s tick size and lot step so the strategy honours exchange constraints without requiring manual normalization.

## Trading logic
1. When the user toggles `PlaceBuyStop` to `true`, the strategy reads the best ask (falling back to the last trade price if necessary) and shifts it by `BuyStopOffsetPoints` converted to price units. A buy stop order with volume `BuyVolume` is submitted at the resulting level. The desired stop-loss and take-profit prices are computed immediately and stored as pending protective targets.
2. When the user toggles `PlaceSellStop` to `true`, the best bid (or last trade) is shifted downward by `SellStopOffsetPoints`. A sell stop order with volume `SellVolume` is placed at that price, and the corresponding protective levels are recorded.
3. After a pending stop order trades, the strategy automatically places the recorded protective orders:
   - Long positions receive a `SellStop` stop-loss below the entry price and a `SellLimit` take-profit above it.
   - Short positions receive a `BuyStop` stop-loss above the entry price and a `BuyLimit` take-profit below it.
   Each protective order is submitted only once; if one fills, the other is cancelled to emulate MetaTrader’s single SL/TP pair.
4. When the `CancelPendingOrders` flag is toggled, any active buy or sell stop orders created by the strategy are cancelled. Protective orders already guarding open positions are intentionally left untouched so ongoing trades remain protected.
5. Volumes are adjusted to the security’s `VolumeStep`, `MinVolume`, and `MaxVolume`. If the resulting size becomes invalid (for example below the minimum lot), the operation is aborted and a warning is logged instead of sending an order.
6. All price distances are expressed in points and converted using the security’s `PriceStep`. If the step is unknown, a conservative fallback of `0.0001` is applied so the panel remains usable on symbols without tick metadata.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BuyVolume` | `decimal` | `0.10` | Volume sent with each buy stop order after respecting the instrument’s lot step. |
| `SellVolume` | `decimal` | `0.10` | Volume sent with each sell stop order after respecting the instrument’s lot step. |
| `BuyStopOffsetPoints` | `int` | `100` | Distance in points added above the current ask to position the pending buy stop. |
| `SellStopOffsetPoints` | `int` | `100` | Distance in points subtracted from the current bid to position the pending sell stop. |
| `BuyStopLossPoints` | `int` | `100` | Stop-loss distance (in points) for long positions triggered by the buy stop. Set to zero to skip the protective order. |
| `SellStopLossPoints` | `int` | `100` | Stop-loss distance (in points) for short positions triggered by the sell stop. Set to zero to skip the protective order. |
| `BuyTakeProfitPoints` | `int` | `150` | Take-profit distance (in points) for long positions triggered by the buy stop. Set to zero to skip the protective order. |
| `SellTakeProfitPoints` | `int` | `150` | Take-profit distance (in points) for short positions triggered by the sell stop. Set to zero to skip the protective order. |
| `PlaceBuyStop` | `bool` | `false` | Toggle that places a buy stop order once. The value resets to `false` automatically after processing. |
| `PlaceSellStop` | `bool` | `false` | Toggle that places a sell stop order once. The value resets to `false` automatically after processing. |
| `CancelPendingOrders` | `bool` | `false` | Toggle that cancels all active pending stop orders created by the panel. |

## Differences from the MetaTrader version
- MetaTrader attaches stop-loss and take-profit levels directly to pending orders. StockSharp keeps the same behaviour by generating dedicated protective orders immediately after an entry fills.
- The StockSharp implementation transparently adapts volumes and prices to the security metadata, removing the need for manual normalization with `_Point`, `_Digits`, or volume rounding.
- Stop-level limitations from the trading venue are not queried automatically. Users should configure offsets that respect the broker’s minimum distance, just as they would in MetaTrader.
- The delete toggle (`CancelPendingOrders`) cancels only pending stops. Existing protective orders for open positions remain active so live trades stay guarded.

## Usage tips
- Assign a portfolio and security before toggling any action flags; otherwise the strategy logs a warning and ignores the request.
- To emulate the original panel workflow, add the strategy to the Designer or Runner UI, expose the parameters in the property grid, and flip the booleans when you want to submit or cancel orders.
- Because the logic relies on best bid/ask quotes, ensure Level 1 data is streamed. If the best prices are missing, the code falls back to the last traded price, but pending orders may end up closer to the market than intended.
- Adjust the point distances to respect the instrument’s minimum stop level. The helper does not automatically enforce broker-specific buffers.
- Set protective distances to zero whenever you want to place naked stop orders without accompanying SL/TP levels.
