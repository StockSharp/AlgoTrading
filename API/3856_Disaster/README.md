# Disaster Strategy (MQL #7704)

## Overview

The original MetaTrader expert advisor named `disaster.mq4` arms stop orders around a very long simple moving average (SMA). It waits until the current price moves far enough away from the average, then parks two pending stop orders that try to capture a mean-reversion snap-back. Each new minute recalculates the SMA and nudges the pending orders to the latest baseline. Filled orders are protected with a fixed stop-loss and an adaptive take-profit that shrinks after the previous trade on the same side closed with a loss.

## Porting notes

* **Data source** – the MQL script uses 1-minute bars through `iMA(PERIOD_M1, 590)`. The StockSharp version subscribes to a configurable candle series (default `TimeSpan.FromMinutes(1)`) and feeds an `SMA` indicator with the same lookback.
* **Trigger logic** – MetaTrader compares the bid/ask quotes to the SMA and requires a 20-pip gap before arming a pending order. The C# port reproduces this by converting the `TriggerDistancePips` parameter to an absolute price distance using the instrument `PriceStep`/`MinPriceStep` plus the 10× multiplier for 3/5-digit FX symbols.
* **Order types** – the EA registers stop orders via `OrderSend(..., OP_BUYSTOP/OP_SELLSTOP, ...)`. StockSharp equivalents are `BuyStop` and `SellStop`. The port keeps both orders independent, letting either remain active if conditions persist.
* **Dynamic relocation** – whenever a new candle arrives the MQL code calls `OrderModify` so that the pending stops track the new SMA. StockSharp achieves the same by calling `ReRegisterOrder` to move active orders without cancel/recreate churn.
* **Stop levels** – MetaTrader enforces broker stop levels (`MODE_STOPLEVEL`). The StockSharp version respects the same safety margin indirectly by rounding to the instrument price step and aborting relocation when the computed price is invalid (≤ 0).
* **Protective orders** – in MT4 the stop-loss and take-profit are attached to the pending order. StockSharp creates separate stop/limit protective orders immediately after an entry fill, mirroring the exact price offsets.
* **Adaptive take-profit** – the EA halves the take-profit distance for the next order if the previous trade on that side lost money. The port maintains `_lastBuyWasLoss` / `_lastSellWasLoss` flags and adjusts the take-profit distance accordingly.
* **Money management** – the script sizes lots with `0.4 * AccountFreeMargin / 1000`, capped by broker limits. The StockSharp port exposes a direct `Volume` parameter and aligns it with `VolumeStep`, `MinVolume`, and `MaxVolume`.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `Volume` | `0.1` | Order volume aligned to the instrument volume step. |
| `MaPeriod` | `590` | Simple moving average length used as the baseline. |
| `StopLossPips` | `30` | Distance between the entry price and the protective stop. |
| `TakeProfitPips` | `70` | Base take-profit distance. Automatically halves after a losing trade on the same side. |
| `TriggerDistancePips` | `20` | Required gap between price and the SMA before arming stop entries. |
| `CandleType` | `1-minute time frame` | Candle series used to feed the SMA. |

All pip-based parameters are translated through the instrument `PriceStep` or `MinPriceStep`. For FX pairs with 3 or 5 decimal digits, the conversion multiplies the step by 10, matching the MetaTrader `Point` behaviour.

## Workflow

1. Subscribe to Level1 quotes and minute candles.
2. Update stored bid/ask prices on each Level1 message.
3. On every finished candle, recalculate the SMA and move any active pending orders to the new baseline.
4. If no position is open and the bid/ask gap exceeds the trigger distance, place the corresponding stop order (sell above the SMA, buy below it when price is undervalued).
5. When a stop order fills, immediately register stop-loss and take-profit orders at the requested distances. Keep track of the last trade result to adapt the next take-profit.
6. Cancel all pending/protective orders when the strategy stops.

## Differences versus the MQL version

* The port relies on StockSharp protective orders instead of broker-attached SL/TP fields. Behaviour is equivalent but uses explicit orders in the account.
* MetaTrader enforces stop-level spacing with `MODE_STOPLEVEL`. StockSharp proxies this requirement by rounding to the available price step and skipping updates when the computed price is invalid. In practice it should respect the same constraints once the adapter validates order prices.
* The original code recalculates the trade volume from free margin every tick. The StockSharp port leaves sizing to the user through the `Volume` parameter for clarity and predictable behaviour across brokers.

## Requirements

* Instruments must expose at least `PriceStep` or `MinPriceStep`. Without them the pip-to-price conversion falls back to `0.0001`, which is appropriate for major FX pairs.
* To mimic FX stop-level rules the data feed should deliver best bid/ask updates (Level1). The strategy gracefully degrades by using the candle close price if quotes are missing.
* Protective orders require brokers/exchanges that support stop and limit orders. If unavailable, adjust the code to fall back to market exits.

## Usage tips

* Start with micro volumes (`0.01`) on demo accounts to validate the price conversions.
* Adjust `TriggerDistancePips` and `TakeProfitPips` together: smaller triggers lead to more frequent trades, so consider lowering take-profit accordingly.
* Monitor the `_lastBuyWasLoss` and `_lastSellWasLoss` flags via logs to confirm that the adaptive take-profit logic matches the MetaTrader history.
