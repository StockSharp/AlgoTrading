# Straddle Trail Strategy

## Overview

The **Straddle Trail Strategy** replicates the behaviour of the original MetaTrader 5 "Straddle&Trail" expert advisor. The strategy places a pair of stop orders (a straddle) around the current price ahead of scheduled news events or immediately on demand. Once a position is triggered the algorithm manages break-even transitions, trailing stops and optional shutdown commands that cancel pending orders or close open positions.

This implementation is built on top of the StockSharp high level API. Order placement, position management and risk controls are implemented without using low-level message processing.

## Trading Logic

1. **Straddle placement**
   * Two stop orders (buy stop above and sell stop below) are created once the scheduled event window is reached or instantly if `PlaceStraddleImmediately` is enabled.
   * Order prices are offset from the current bid/ask by `DistanceFromPrice` (expressed in pips). The offset is converted into price units using the instrument price step.
   * The strategy prevents re-creating the straddle multiple times on the same day unless the orders are adjusted or explicitly cancelled.

2. **Pre-event order management**
   * When `AdjustPendingOrders` is enabled the stop orders are cancelled and re-placed every new minute so they stay aligned with the current price.
   * Adjustments stop `StopAdjustMinutes` before the event to avoid chasing the price when volatility rises.
   * If `RemoveOppositeOrder` is enabled the remaining stop order is automatically cancelled once one side of the straddle triggers and opens a position.

3. **Risk management**
   * Initial stop-loss and take-profit levels are calculated from `StopLossPips` and `TakeProfitPips` and are tracked internally.
   * When the open profit reaches `BreakevenTriggerPips` the stop level is moved to the entry price plus `BreakevenLockPips` (or the symmetric value for short trades).
   * If `TrailPips` is greater than zero a trailing stop follows the price. Trailing can start immediately or only after the break-even condition depending on `TrailAfterBreakeven`.
   * Profit taking and stop exits are executed with market orders for reliability.

4. **Manual shutdown**
   * Setting `ShutdownNow` to `true` triggers an immediate cleanup according to the `ShutdownMode` option. Possible actions include closing long/short positions and cancelling pending long/short orders.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `ShutdownNow` | Triggers the shutdown procedure on the next candle update. Automatically resets to `false` after execution. |
| `ShutdownMode` | Defines what should be cancelled or closed (`All`, `LongPositions`, `ShortPositions`, `PendingLong`, `PendingShort`). |
| `DistanceFromPrice` | Distance between the current price and each stop order, measured in pips. |
| `StopLossPips` | Initial stop-loss distance for triggered positions. Set to `0` to disable. |
| `TakeProfitPips` | Initial take-profit distance. Set to `0` to disable. |
| `TrailPips` | Trailing stop distance. Set to `0` to disable trailing. |
| `TrailAfterBreakeven` | When `true`, trailing starts only after the break-even condition is satisfied. |
| `BreakevenLockPips` | Profit locked when the break-even trigger activates. |
| `BreakevenTriggerPips` | Profit threshold that activates the break-even logic. |
| `EventHour` / `EventMinute` | Scheduled event time (broker/server time). Set both to `0` to disable the event scheduler. |
| `PreEventEntryMinutes` | Minutes before the event when the straddle should be placed. Ignored when the event is disabled or when immediate placement is enabled. |
| `StopAdjustMinutes` | Number of minutes before the event when auto-adjustment of pending orders stops. |
| `RemoveOppositeOrder` | Cancels the unfilled stop order when the first leg of the straddle triggers. |
| `AdjustPendingOrders` | Enables automatic re-centring of pending orders while waiting for the event. |
| `PlaceStraddleImmediately` | Places the straddle right after the strategy starts, bypassing the event schedule. |
| `CandleType` | Candle subscription used for time tracking. Defaults to 1-minute candles. |

> **Volume** – the StockSharp `Volume` property controls order size. It is set to `1` by default and can be modified before starting the strategy.

## Data Subscriptions

The strategy subscribes to:

* The configured candle series (default 1-minute) to run the scheduler, trailing logic and shutdown checks.
* The order book to keep track of the latest bid/ask prices for precise stop order alignment.

## Notes and Limitations

* Stop-loss and take-profit management is executed via market orders rather than by modifying broker-side protective orders. This mirrors the original behaviour while keeping the implementation simple.
* The strategy uses the instrument `PriceStep` to approximate pip size. For exotic instruments adjust parameters accordingly.
* The shutdown command is evaluated only when new candle data arrive. For immediate action reduce the candle timeframe.
* Python implementation is intentionally omitted as requested.

## Conversion Notes

* The break-even and trailing logic is ported line-by-line from the MQL version. The StockSharp version maintains the same numeric relationships but operates on decimal prices and uses market exits.
* Manual trade handling (magic number `0` in MQL) is not reproduced because StockSharp strategies manage their own positions. All protective logic applies to strategy-generated trades only.
* The `CalcMagic` function is unnecessary in StockSharp and was therefore removed. Strategy state is tracked internally by the framework.

