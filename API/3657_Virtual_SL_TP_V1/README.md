# Virtual SL TP V1 Strategy

The **Virtual SL TP V1 Strategy** is a StockSharp port of the MetaTrader script `Virtual_SL_TP_Pending_with_SL_Trailing.mq4` (MQL ID 49146). The original script managed an open long position by tracking virtual stop-loss / take-profit levels and, when requested, arming a trailing pending order once price momentum picked up. This C# version keeps the same behaviour while relying purely on StockSharp high-level APIs (`SubscribeLevel1`, `BuyStop`, `ClosePosition`).

## Core ideas

1. **Spread-aware risk control** – the initial spread is captured on start-up. If the current spread widens beyond the configured threshold, every virtual level shifts upward to keep the same relative distance from the market.
2. **Virtual exits** – the strategy never sends stop or take-profit orders. Instead it monitors the best bid and calls `ClosePosition()` whenever the virtual stop-loss or virtual take-profit level is crossed.
3. **Optional trailing order** – when `EnableTrailing` is `true` and the best ask reaches the trigger price, a buy-stop order is placed at the virtual pending level. Order price is automatically refreshed if the spread adjustment moves the trigger.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `StopLossPoints` | `20` | Distance between the ask price and the virtual stop-loss in MetaTrader points. |
| `TakeProfitPoints` | `40` | Distance between the ask price and the virtual take-profit in MetaTrader points. |
| `SpreadThreshold` | `2.0` | Additional spread (in MetaTrader points) required before all virtual levels are shifted upward. |
| `TrailingStopPoints` | `10` | Offset (in MetaTrader points) between the ask price and the pending buy-stop trigger. |
| `EnableTrailing` | `false` | Enables or disables the trailing pending order logic. |

> **MetaTrader points** are derived automatically from `Security.PriceStep`. If the price step is missing, the strategy defaults to `1`.

## Execution flow

1. `SubscribeLevel1()` feeds best bid/ask updates into the strategy.
2. The very first tick stores the initial spread and builds three virtual levels: stop-loss, take-profit and pending order trigger.
3. Every subsequent update:
   - Re-calculates the spread. If it exceeds `initialSpread + SpreadThreshold`, all virtual levels are shifted upward by the same absolute distance.
   - Checks the best bid against the virtual stop-loss / take-profit and calls `ClosePosition()` if either threshold is crossed.
   - When trailing is enabled, compares the best ask with the pending trigger and sends a `BuyStop` order once the trigger is touched.
4. Pending orders are cancelled automatically when trailing mode is disabled or the strategy stops.

## Files

- `CS/VirtualSlTpV1Strategy.cs` – StockSharp strategy implementation.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.
