# 21hour Strategy

## Overview
The **21hour** strategy reproduces the behaviour of the MQL4 expert advisor `21hour.mq4`. It operates around a daily time window: pending breakout orders are created at a configurable start hour and all exposure is removed at a configurable stop hour. The StockSharp version keeps the same "two stop orders around the price" idea while leveraging the high-level API for order management, market data subscriptions and protective take-profit handling.

## Trading Logic
- At the start of every trading day, when the server time matches `StartHour:00`, the strategy reads the latest bid/ask quotes and places both a buy stop and a sell stop order.
  - The distance from the current ask price to the buy stop trigger is `StepPoints * PriceStep`.
  - The distance from the current bid price to the sell stop trigger is the same amount below the market.
  - `TakeProfitPoints` is converted to price distance through the instrument price step and passed to `StartProtection`, so both long and short positions receive a protective take-profit right after execution.
- Only one pending setup is allowed per day. If only one of the two stop orders remains active (for example, after one side was filled), the strategy cancels the surviving pending order to mirror the original EA logic.
- When the clock reaches `StopHour:00`, the strategy closes any open position at market and cancels all outstanding pending orders. This applies even if no breakout occurred.
- The default candle stream is one-minute data. It is used purely to trigger the hourly checks on finished candles, which mimics the `prevtime` guard from the MQL version.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Volume` | Order volume in lots for both pending orders. | `0.1` |
| `StartHour` | Hour (0–23) when the pair of pending orders is created. | `10` |
| `StopHour` | Hour (0–23) when the strategy closes positions and removes all pending orders. | `22` |
| `StepPoints` | Distance in instrument points between the current bid/ask price and each stop entry. Converted to price by multiplying with `PriceStep`. | `15` |
| `TakeProfitPoints` | Distance in points from the entry price to the take-profit target managed by `StartProtection`. Set to `0` to disable the target. | `200` |
| `CandleType` | Candle data type used for time tracking. Default is one-minute time frame (`TimeSpan.FromMinutes(1).TimeFrame()`). | `1 minute` |

## Implementation Notes
- Uses `SubscribeCandles` to obtain finished candles and evaluate the hourly schedule only once per minute.
- Subscribes to level-1 quotes via `SubscribeLevel1()` to keep the latest bid/ask values for accurate stop placement.
- Relies on `StartProtection` with a take-profit unit to emulate the pending-order take-profit from the original EA instead of manually attaching orders.
- Keeps track of the active buy and sell stop orders and calls `CancelOrder` if only one side remains, ensuring the system never runs with an unpaired pending order.
- Invokes `BuyMarket` / `SellMarket` helpers for flatting positions at the stop hour, strictly using the high-level Strategy API.

## Behaviour Notes
- The strategy expects the broker connection to provide price step information. If `PriceStep` is absent, prices are left unrounded.
- Pending orders are generated only once per calendar day. They will be re-created on the next trading day at the configured start hour even if the previous day's breakout did not trigger.
- When `TakeProfitPoints` is zero the strategy still places pending orders but no protective take-profit is managed.
