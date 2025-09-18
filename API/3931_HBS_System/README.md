# HBS System Strategy (StockSharp Version)

## Overview

The **HBS System Strategy** is a high-level StockSharp conversion of the MetaTrader 4 expert advisor "HBS system.mq4" (ForTrader.ru). The original EA combines exponential moving average filtering with pending stop orders that are rounded to fixed price levels. Two stop orders are deployed in the trend direction: the first targets a nearby rounded level and the second seeks an extended breakout. Both trades share the same protective stop and trailing logic, producing a layered breakout structure.

This StockSharp port keeps the multi-order behaviour while embracing the high-level API. Orders are submitted through the pending order helpers (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) and risk is controlled via dynamically maintained protective stops. The code is fully commented in English for easier maintenance.

## Trading Logic

1. **Trend Filter** – An exponential moving average (EMA) calculated on the median price (`(High + Low) / 2`) of completed candles defines the active trend. Only fully formed candles are processed, mirroring the `iMA(..., shift=1)` behaviour from MetaTrader.
2. **Level Rounding** – The closing price of the previous candle is rounded up and down using a configurable multiplier (default `100`, i.e. two decimals). These rounded values emulate the original `MathCeil`/`MathFloor` calls.
3. **Entry Construction** – When the previous candle opens and closes above the EMA, two buy stop orders are placed:
   - **Primary order** at `roundedHigh - entryOffset` with a take-profit equal to the rounded level.
   - **Secondary order** at the same entry price but with a take-profit shifted further by `secondaryTakeProfitPoints`.
   - Both orders share a common stop-loss (`entry - stopLossPoints`).

   The logic is mirrored for shorts when the candle opens and closes below the EMA. Opposite pending orders are cancelled automatically to prevent overlap.
4. **Position Management** – When a pending order fills, the strategy registers a dedicated take-profit limit order and updates the shared stop-loss. Trailing stop logic tightens the stop when price moves in favour of the open position, respecting the configured trailing distances.
5. **Cleanup** – Completed or cancelled orders are removed from the internal registry. When the net position returns to flat, all protective orders are cancelled to reset the state.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `EMA Period` | Length of the exponential moving average filter. | 200 |
| `Buy Stop-Loss (points)` | Distance (in points) between the long entry and its protective stop. | 50 |
| `Buy Trailing (points)` | Trailing distance for long positions. | 10 |
| `Sell Stop-Loss (points)` | Distance (in points) between the short entry and its protective stop. | 50 |
| `Sell Trailing (points)` | Trailing distance for short positions. | 10 |
| `Order Volume` | Volume applied to **each** pending order. With the default two orders, the maximum exposure equals twice this value. | 0.1 |
| `Entry Offset (points)` | Offset (in points) subtracted/added from the rounded level to obtain the pending entry price. | 15 |
| `Second Take-Profit (points)` | Additional distance used by the secondary take-profit target. | 15 |
| `Rounding Factor` | Multiplier used for the rounding logic (e.g., 100 → two decimal places). | 100 |
| `Candle Type` | Data type for candle aggregation. Defaults to a 1-hour timeframe. | `TimeFrame(1h)` |

## Notes for Usage

- Ensure the `Security.PriceStep` (or `Security.Decimals`) is configured; otherwise, the strategy falls back to a 0.0001 point value.
- Each pending order manages its own take-profit, so the total position may scale out in two stages.
- Trailing stops only activate after the price has moved in favour by the configured distance (`TrailingStop{Buy/Sell}Points`).
- The strategy assumes traditional forex-style pricing where rounding to two decimal places is meaningful. Adjust the `RoundingFactor` if a different precision is required.
- No automated money-management rules are included; set `OrderVolume` according to risk preferences.

## Conversion Highlights

- All comments were rewritten in English and the structure follows the repository style guide (tabs, namespace, naming).
- High-level StockSharp helpers are used for data subscription, pending order management, and protective order handling.
- Trailing stop and take-profit coordination reproduces the dual-order architecture of the original MetaTrader expert while remaining idiomatic to StockSharp.

## References

- Original MT4 script: `MQL/8134/HBS_system.mq4`
- StockSharp documentation: [https://doc.stocksharp.com/](https://doc.stocksharp.com/)
