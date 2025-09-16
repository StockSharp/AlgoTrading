# Hist Training Strategy

## Overview
The Hist Training strategy is a StockSharp high level implementation of the MetaTrader helper "HistoryTrain". The original MQL script reacted to manually drawn horizontal lines and external DLL flags to stage breakout, pullback and market orders. This port replaces that workflow with explicit parameters so the same behaviour can be reproduced without chart objects or shared memory. The strategy keeps track of three price levels and automatically manages stop, limit or market entries according to the selected configuration.

A candle subscription (one minute by default) is used to evaluate the relationship between the latest closing price and the configured levels. As soon as the required conditions are satisfied the strategy creates the appropriate pending order or fires an immediate market order, closely mirroring the trigger structure of the original expert while leveraging the StockSharp order helpers.

## Level mapping
* **Upper level** – acts as take profit for long positions and stop loss for short positions. When breakout stop orders are enabled it also defines the activation price of short entries.
* **Entry level** – the central anchor used for buy/sell stop and limit orders as well as the reference for market-triggered entries. In the MQL script this corresponded to the lime coloured middle horizontal line.
* **Lower level** – plays the opposite role of the upper level: it is used as stop loss for long positions, take profit for short positions and, when breakout orders are enabled, it becomes the activation price for long entries.

## Order logic
* **Breakout mode** (`UseBreakoutOrders`) – when the price is below the entry level and long trading is enabled a buy stop is armed at the entry price. When the price is above the entry level and short trading is enabled a sell stop is armed. Volumes are normalised to the instrument step and the routine ensures only one active order per direction.
* **Pullback mode** (`UsePullbackOrders`) – when the price is above the entry level the strategy prepares a buy limit, anticipating a retracement. When the price is below the entry level it prepares a sell limit. Breakout and pullback orders can operate simultaneously, reproducing the combinations controlled by flags `25` and `26` in the original code.
* **Market triggers** (`EnableMarketBuy`, `EnableMarketSell`) – when armed, the strategy watches for a close above (for buys) or below (for sells) the entry level while flat. As soon as the condition is met it enters at market, cancels any pending orders and stores the protection levels defined by the upper/lower bands.

Pending orders are re-created whenever their price diverges from the configured entry level or when they are cancelled or executed. The helper also cleans up stale references so that completed MetaTrader-like behaviours (for example deleting the lime lines) are emulated by cancelling the orders.

## Position management
Once a position is open the strategy deactivates every pending order and monitors the candle highs and lows:

* For a **long** position the lower level is treated as a stop loss and the upper level as a take profit. A hit on either level immediately closes the position at market.
* For a **short** position the upper level becomes the stop loss and the lower level the take profit. The same market exit logic is used to emulate the built-in stop/take values passed to `OrderSend` in the MQL version.

The protection values are recalculated if the position direction changes and are reset when the exposure returns to zero. This preserves the behaviour of the MT4 tool that cleared the horizontal lines after filling an order.

## Parameters
| Name | Description |
| --- | --- |
| `UpperLevel` | Price level used as take profit for longs and stop loss for shorts. |
| `EntryLevel` | Central price that anchors all orders and triggers. |
| `LowerLevel` | Price level used as stop loss for longs and take profit for shorts. |
| `EnableLongSide` | Allows the strategy to submit long side orders. Disable to trade the short side only. |
| `EnableShortSide` | Allows the strategy to submit short side orders. Disable to trade the long side only. |
| `UseBreakoutOrders` | Enables the stop-order breakout workflow reminiscent of `OrderSend(..., OP_BUYSTOP/OP_SELLSTOP)` from the MQL code. |
| `UsePullbackOrders` | Enables the limit-order pullback workflow reminiscent of `OrderSend(..., OP_BUYLIMIT/OP_SELLLIMIT)`. |
| `EnableMarketBuy` | Arms an automatic market buy when a candle closes above the entry level while the strategy is flat. |
| `EnableMarketSell` | Arms an automatic market sell when a candle closes below the entry level while the strategy is flat. |
| `OrderVolume` | Base volume for every new order. The strategy normalises it to the instrument volume step. |
| `CandleType` | Data type used to monitor the instrument (one minute candles by default). |

## Conversion notes
* The DLL-driven shared variables (`GetInt@4`, `SetInt@8`, etc.) have been replaced with strongly typed strategy parameters. Every flag from the MQL script now maps to an explicit property that can be configured or optimised inside StockSharp.
* Horizontal line discovery is no longer required; instead the three key levels are provided directly. This avoids reliance on chart objects but preserves the intent of selecting up to three lime coloured lines.
* Order placement uses the high level helpers (`BuyStop`, `SellLimit`, etc.) to guarantee proper rounding and integration with the platform order book.
* Protective stop loss and take profit levels are implemented via candle-based monitoring so they remain effective even on venues that do not support native SL/TP attachments.
* Extensive logging mirrors the feedback previously displayed via `Print` statements, making it easier to audit the strategy timeline inside the StockSharp log viewer.
