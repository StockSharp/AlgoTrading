# Sprut Pending Order Grid Strategy

## Overview
The **Sprut Pending Order Grid Strategy** reproduces the MetaTrader 5 expert advisor *Sprut (barabashkakvn's edition)* inside StockSharp's high-level strategy framework. It builds a configurable grid of buy and sell pending orders around the current market price and manages every order's lifetime, volume scaling, and post-fill protection using StockSharp's helper methods (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).

The converted version keeps the original expert advisor philosophy:

* place the very first order for each enabled direction at either a manual price or at an automatic offset measured in pips from the best quote;
* extend the grid step-by-step using independent spacing for stop and limit orders;
* scale order volumes using a multiplier that mirrors the MT5 implementation;
* arm each filled order with its own stop-loss and take-profit, expressed as pip offsets from the entry price;
* enforce global profit and loss checkpoints that immediately liquidate positions and remove any remaining pending orders when hit;
* optionally expire pending orders after a specified number of minutes.

## How the strategy works
1. **Market data** – the strategy subscribes to order book updates to track the best bid/ask and to candles (default 1 minute) to run periodic maintenance. No indicators are required.
2. **Grid initialization** – when there is no open position and no active grid order, the strategy computes the initial price for each of the four possible order types:
   * **Buy Stop**: best ask + `DeltaFirstBuyStop` (unless `FirstBuyStop` is non-zero).
   * **Buy Limit**: best bid − `DeltaFirstBuyLimit` (unless `FirstBuyLimit` is non-zero).
   * **Sell Stop**: best bid − `DeltaFirstSellStop` (unless `FirstSellStop` is non-zero).
   * **Sell Limit**: best ask + `DeltaFirstSellLimit` (unless `FirstSellLimit` is non-zero).
   Each offset is converted from pips using the security `PriceStep` (fallback: 0.0001).
3. **Order stacking** – for each enabled direction the strategy creates `CountOrders` entries separated by `StepStop` or `StepLimit` (also in pips). Volumes follow the original formula: order #0 uses the base volume, while order #N uses `baseVolume * N * coefficient` whenever the coefficient is greater than 1. Volumes are adjusted to respect `Security.VolumeStep`, `Security.MinVolume`, and `Security.MaxVolume`.
4. **Expiration** – if `ExpirationMinutes` is positive, the strategy timestamps every pending order and cancels it automatically after the deadline.
5. **Protection after fill** – when StockSharp reports that an entry order is done, the strategy registers the matching stop-loss and take-profit orders (`StopLoss` and `TakeProfit` in pips). A zero distance disables the respective protection leg.
6. **Profit checkpoint** – realised plus unrealised PnL are recalculated whenever new data arrives. If `ProfitClose` is positive and reached, or `LossClose` (typically negative) is breached, the strategy requests a full liquidation: market-closes the position, cancels all grid orders, and cancels remaining protection orders. Trading resumes automatically after everything is flat again.
7. **Continuous maintenance** – every update cleans finished orders, removes expired items, attempts to place a fresh grid when conditions allow, and avoids rearming while a liquidation is in progress.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CountOrders` | Number of orders per enabled direction. | 5 |
| `FirstBuyStop`, `FirstBuyLimit`, `FirstSellStop`, `FirstSellLimit` | Optional absolute prices for the first order in each direction (0 = use automatic offset). | 0 |
| `DeltaFirstBuyStop`, `DeltaFirstBuyLimit`, `DeltaFirstSellStop`, `DeltaFirstSellLimit` | Pip offsets applied to the best bid/ask when automatic pricing is used. | 15 |
| `UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit` | Enable or disable each grid direction. | false |
| `StepStop`, `StepLimit` | Distance between consecutive stop or limit orders (pips). | 50 |
| `VolumeStop`, `VolumeLimit` | Base volume for the first stop/limit order. | 0.01 |
| `CoefficientStop`, `CoefficientLimit` | Multiplier applied to additional orders (>1 keeps the MT5 scaling behaviour). | 1.6 |
| `ProfitClose` | Total PnL threshold that triggers liquidation (monetary units). | 10 |
| `LossClose` | Total PnL floor that triggers liquidation (monetary units, typically negative). | -100 |
| `ExpirationMinutes` | Pending-order lifetime in minutes (0 = good-till-cancel). | 60 |
| `StopLoss`, `TakeProfit` | Pip distances for protective stop/take orders created after a fill (0 disables). | 50 / 0 |
| `CandleType` | Candle series used for periodic maintenance. | 1 minute candles |

## Usage notes
* Enable at least one of the four boolean switches (`UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit`) to allow the grid to be created.
* The pip conversion relies on the security `PriceStep`. Instruments with exotic tick sizes may require adjusting the offsets for equivalent behaviour.
* `ProfitClose`/`LossClose` compare the sum of realised PnL (`Strategy.PnL`) and the unrealised PnL computed from the latest best bid/ask; make sure the step price metadata is filled for the traded instrument.
* Protective stop and take orders are independent StockSharp orders; if you manually close a position outside the strategy, the remaining protection orders are cancelled when the net position returns to zero.
* The `CandleType` parameter only controls how often maintenance runs; order placement still reacts immediately to order-book updates.

## Differences from the MT5 expert advisor
* Position accounting is netted: StockSharp keeps a single net position per security similar to the MT5 netting regime.
* Instead of MT5's built-in stop-loss/take-profit fields on pending orders, StockSharp protection orders are created only after an entry order is executed.
* Volume normalisation uses `Security.VolumeStep`, `MinVolume`, and `MaxVolume`; verify these values when trading CFDs or crypto exchanges.
* The strategy does not expose a separate *close all* button—the liquidation routine is fully automatic through the PnL thresholds, matching the original expert logic where `ProfitClose`/`LossClose` toggled a full shutdown.

## Getting started
1. Assign the strategy to a connector that supplies at least order book data and candlesticks for the chosen `CandleType`.
2. Configure the four directional switches and volume parameters to match your risk profile.
3. Define stop-loss/take-profit distances when protective orders are required (set to zero to disable).
4. Adjust `ProfitClose`/`LossClose` to values consistent with your account currency.
5. Start the strategy; it will wait for the first order book snapshot before building the grid.

> **Python version** – not provided. Only the C# implementation is included, as requested.
