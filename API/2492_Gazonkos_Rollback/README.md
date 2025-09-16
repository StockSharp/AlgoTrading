# Gazonkos Rollback Strategy

## Overview
The Gazonkos Rollback Strategy is a conversion of the original **gazonkos** MetaTrader 5 expert advisor. The approach trades the EUR/USD hourly chart and looks for strong momentum between two historical closes. After detecting that momentum, it waits for a pullback of a predefined size and then enters in the direction of the initial move. The StockSharp implementation keeps the same staged state machine as the source code while using the high-level API with candle subscriptions and protective orders.

## Trading Logic
1. **Eligibility check** – only one position per hour is allowed. If another trade was opened during the same clock hour, or if the configured number of simultaneous trades is already running, the strategy waits.
2. **Momentum detection** – compares the closing prices of two past candles (`SecondShift` minus `FirstShift`). If the difference exceeds `Delta`, the strategy records the intended direction (long if the newer close is higher, short otherwise).
3. **Rollback tracking** – from the moment the momentum appears, the code monitors the highest high (for long setups) or lowest low (for short setups) reached during that hour. When price pulls back by at least `Rollback`, the setup becomes eligible for execution. If the hour changes before the pullback happens, the signal is discarded.
4. **Order execution** – once the rollback condition is met, the strategy places a market order with fixed take profit and stop loss distances. Position sizing is controlled through the `TradeVolume` parameter, and the built-in `StartProtection` helper manages the protective orders.

This sequence closely mirrors the MT5 version that used `STATE` and `Trade` variables to coordinate the workflow.

## Risk Management
* `StartProtection` configures fixed take profit and stop loss distances in absolute price units, similar to how the expert attached TP/SL to each order.
* `ActiveTrades` limits the maximum total exposure by comparing the absolute position value to the product of the configured volume and allowed trade count.
* The combination of hourly gating and rollback confirmation reduces over-trading during sideways conditions.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TakeProfit` | `0.0016` | Absolute distance (in price units) for the take profit. Matches 16 points on a 5-digit EUR/USD quote. |
| `Rollback` | `0.0016` | Required pullback from the extreme reached after the momentum signal. |
| `StopLoss` | `0.0040` | Absolute distance for the protective stop loss. Equivalent to 40 points on EUR/USD. |
| `Delta` | `0.0040` | Minimum difference between the two historical closes that defines a strong move. |
| `TradeVolume` | `0.1` | Default order volume passed to `BuyMarket()` and `SellMarket()`. |
| `FirstShift` | `3` | Older bar index (number of candles back) used for the closing price comparison. |
| `SecondShift` | `2` | Newer bar index used in the closing price comparison. |
| `ActiveTrades` | `1` | Maximum number of simultaneous trades. Set to zero to disable the limit. |
| `CandleType` | `1 hour` time frame | Candle series used for analysis; defaults to hourly candles like the source EA. |

## Notes
* The strategy works with any instrument that has a reasonable tick size; adjust `Delta`, `Rollback`, `TakeProfit`, and `StopLoss` to match the instrument's point value.
* All inline comments are written in English as required by the project guidelines.
* No Python port is provided for this strategy yet.
