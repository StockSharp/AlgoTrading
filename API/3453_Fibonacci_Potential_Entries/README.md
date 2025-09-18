# Fibonacci Potential Entries Strategy

## Overview

This strategy reproduces the behaviour of the original **EA_PUB_FibonacciPotentialEntries** expert advisor. It places two limit orders at the 50% and 61% Fibonacci retracement levels and manages their lifecycle using the high-level StockSharp API.

## Trading Logic

1. **Initial placement**
   - As soon as bid/ask quotes are available the strategy calculates the current spread and submits two limit orders:
     - Order #1: placed at the 50% level with a protective stop below (or above for shorts) the 61% level.
     - Order #2: placed at the 61% level with a protective stop placed half way towards the 100% level.
   - Volumes are sized so that the first trade risks 0.7% of the portfolio and the second trade risks the remaining part of the `RiskPercent` parameter.

2. **Target handling**
   - When price reaches the `TargetPrice` level the strategy closes half of each filled position using market orders.
   - After partial exit the remaining volume is protected at break-even (entry price). If the market returns to that level the rest of the position is closed automatically.

3. **Direction**
   - `IsBullish = true` creates buy limits (original bullish template).
   - `IsBullish = false` mirrors the behaviour with sell limits and inverted stop/target checks.

## Parameters

| Name | Description |
|------|-------------|
| `PriceOn50Level` | Price level for the first limit order. |
| `PriceOn61Level` | Price level for the second limit order. |
| `PriceOn100Level` | Reference level used to compute the second trade stop. |
| `TargetPrice` | Shared profit target for both positions. |
| `RiskPercent` | Total percentage of portfolio equity risked across both entries. |
| `IsBullish` | Chooses between long and short setups. |

## Conversion Notes

- Only high-level helpers (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyMarket`, `SellMarket`) are used, exactly as required by the repository guidelines.
- Partial exits and break-even stop adjustments are reproduced with market orders, matching the MQL robot behaviour without relying on low-level order modification calls.
- Position volumes are normalised to the instrument volume step to stay consistent with StockSharp conventions.
