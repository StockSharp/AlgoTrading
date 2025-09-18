# Omzdwwi Pending Manager Strategy

## Overview

The **Omzdwwi Pending Manager Strategy** is a direct high-level StockSharp translation of the MetaTrader 4 expert `omzdwwi7739cyjayvs_1_65.mq4`. The original advisor focuses on maintaining a ring of pending orders around the current market price, executing market entries on a scheduled timer, and managing trailing stops for both active positions and outstanding pending orders. This C# version reproduces the same logic while leveraging StockSharp's `Strategy` API, `SubscribeLevel1` feed, and order management helpers (`BuyStop`, `SellLimit`, `ReRegisterOrder`, etc.).

The strategy continuously:

- Keeps up to four pending orders (buy stop, sell stop, buy limit, sell limit) at configurable distances from bid/ask quotes.
- Optionally fires market buy/sell orders at a specific hour and minute.
- Applies multiple layers of exits for market positions: fixed take-profit, fixed stop-loss, additional "pips profit" target, and trailing stop logic that mimics the expert's `TrailingPositions()` routine.
- Moves pending orders closer or further from price according to the expert's `TrailingOtlozh()` rules once the market advances by the configured trailing distance.
- Monitors account-level profit and loss thresholds, emitting info/warning logs when the configured global take-profit or stop-loss percentages are reached.

## Signal flow and data subscriptions

- `SubscribeLevel1()` delivers bid/ask updates. Each quote update triggers time checks, order placement, trailing adjustments, and exit checks. No candle data or indicators are required.
- `GetWorkingSecurities()` declares the level-1 subscription so the strategy may run in both live and backtesting environments.

## Entry logic

1. **Scheduled market orders.** When `UseTimeSignals` is enabled and the server clock reaches `SignalHour:SignalMinute`, the strategy raises boolean latches derived from the `Time*Signal` parameters. The next level-1 update calls `BuyMarket()` or `SellMarket()` provided `WaitClose`/`MaxMarketOrders` allow it. Latches reset immediately after the trade.
2. **Persistent pending orders.** For each enabled order type (`EnableBuyStop`, `EnableSellStop`, `EnableBuyLimit`, `EnableSellLimit`) the strategy verifies there is an active order. Absent orders are placed at `Distance * PriceStep` points from the best bid/ask, replicating the expert's `UstanOtlozh()` behaviour. If the order already exists, `ReRegisterOrder` keeps the price aligned to current quotes.

## Exit logic for market positions

- **Fixed stop-loss / take-profit** come from `MarketStopLossPoints` and `MarketTakeProfitPoints`. When the best bid/ask crosses those thresholds the position is flattened via market order.
- **Additional pips target** replicates the expert's `PipsProfit` behaviour. When non-zero, it closes the position after earning the configured profit even if TP is disabled.
- **Trailing stop** copies `TrailingPositions()`. Once the position is sufficiently profitable (or immediately if `RequireProfitBeforeTrailing=false`), the internal trailing price is updated to `Bid - MarketTrailingOffsetPoints * PriceStep` for longs and `Ask + MarketTrailingOffsetPoints * PriceStep` for shorts with the minimum trail step enforced by `MarketTrailingStepPoints`.

## Trailing logic for pending orders

- Stop orders use `StopTrailingOffsetPoints` and `StopTrailingStepPoints`. When price crosses the MQL threshold (`Ask < OrderPrice - (offset + step)` for buy stops, symmetrical for sells) the order is re-registered to `Ask + offset` or `Bid - offset`.
- Limit orders use `LimitTrailingOffsetPoints` and `LimitTrailingStepPoints` in the same manner, recreating the `TrailingOtlozh()` adjustments.

## Risk & account monitoring

- `MaxMarketOrders` limits how many lots (expressed in multiples of `OrderVolume`) can be accumulated per direction when `WaitClose=false`.
- `UseGlobalLevels`, `GlobalTakeProfitPercent`, and `GlobalStopLossPercent` watch portfolio equity. When thresholds are exceeded the strategy writes an info or warning log, mirroring the original alert pop-ups.

## Parameters

| Group | Parameter | Description |
|-------|-----------|-------------|
| General | `OrderVolume` | Trade volume (lots) reused by every order. |
| Execution | `WaitClose` | Block new entries until the net position is flat. |
| Execution | `MaxMarketOrders` | Maximum concurrent lots per direction when pyramiding is allowed. |
| Pending Orders | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | Enable or disable each pending order type. |
| Pending Orders | `StopStepPoints`, `LimitStepPoints` | Distance in points used to place stop/limit orders relative to the current bid/ask. |
| Pending Orders | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | Protective distances applied once pending orders trigger. |
| Pending Orders | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | Trailing parameters for outstanding pending orders. |
| Market Risk | `MarketTakeProfitPoints`, `MarketStopLossPoints` | Take-profit and stop-loss in points for market positions. |
| Market Risk | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | Trailing stop configuration for market positions. |
| Market Risk | `ExitProfitPoints` | Additional fixed profit target. |
| Time Management | `UseTimeSignals`, `SignalHour`, `SignalMinute` | Scheduled execution settings. |
| Time Management | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | Which orders to trigger when the timer fires. |
| Account Monitoring | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Portfolio-level alert thresholds. |
| Misc | `SlippagePoints` | Reserved legacy parameter maintained for completeness. |

## Conversion notes

- The MQL expert set take-profit/stop-loss directly on pending orders. StockSharp places the pending entry first and then manages exits through strategy logic to keep the implementation within high-level API constraints.
- Sound alerts were omitted because StockSharp logging already provides structured notifications.
- MetaTrader's `MODE_STOPLEVEL` constraint does not exist in StockSharp; therefore the parameters rely on the trader to respect exchange-imposed minimum distances.
- Error handling uses `AddInfoLog`/`AddWarningLog` instead of `Alert()` popups.

## Usage

1. Attach the strategy to a `Security` and `Portfolio` with a valid price step.
2. Configure distances in points (they are automatically converted to price units using the security's `ShrinkPrice`).
3. Start the strategy; it will subscribe to level-1 quotes and begin managing orders immediately.

> **Tip:** When backtesting, ensure the tester feeds level-1 data so that the trailing and timing logic receive updates on every quote just like the original MQL expert.
