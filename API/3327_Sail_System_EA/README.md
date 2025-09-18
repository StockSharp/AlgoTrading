# Sail System EA Strategy

## Overview
Sail System EA is a hedging scalper that keeps a symmetrical long/short exposure while
constantly checking broker requirements such as maximum spread, minimal stop level and
trading session limits. The StockSharp port recreates the original behaviour with the
high-level `Strategy` API: the engine subscribes to level-1 quotes, opens or re-arms
both sides of the hedge, and manages virtual stop-loss/take-profit levels without using
low-level connector calls.

The implementation keeps two internal `PositionState` objects (long and short). For each
side the strategy tracks entry price, remaining volume, virtual protection levels and
pending orders. This mirrors the MQL expert that maintained separate ticket counters for
market and pending orders.

## Trading Logic
1. **Session filter.** Trading can be restricted to a configurable time window. When the
   current time falls outside the session the strategy either keeps, cancels or closes
   existing exposure depending on the `ManageExistingOrders` parameter.
2. **Spread watchdog.** Bid/ask updates are collected through `SubscribeLevel1()`. The
   strategy either checks the instantaneous spread or a rolling average (up to 100
   samples) and compares the value to `MaxSpread` plus the configured commission. If the
   spread is too wide, the system optionally closes open positions and the entry
   distance can be multiplied by `MultiplierIncrease` to wait for calmer conditions.
3. **Entry engine.** When trading is allowed the strategy either opens both sides with
   market orders or maintains paired limit orders, depending on `UsePendingOrders`. The
   limit price for new orders is derived from the current best bid/ask plus
   `DistancePending` (in pips) and an optional safety multiplier.
4. **Virtual protection.** Each fill sets virtual stop-loss and optional take-profit
   levels using `OrdersStopLoss` / `OrdersTakeProfit`. Virtual levels are recalculated
   after `DelayModifyOrders` quote updates, but only when the improvement is larger than
   `StepModifyOrders`. The update mechanism reproduces the gradual stop adjustments from
   the MQL version without calling `OrderModify`.
5. **Exit handling.** When the bid (for longs) or ask (for shorts) reaches the virtual
   stop or target, the strategy sends the opposite market order to close the position.
   Exits are labelled according to the reason (stop loss, take-profit, session end or
   spread violation) so the resulting trade log matches the expert advisor output.
6. **Re-entry management.** If pending orders drift away from the market by more than
   `PipsReplaceOrders` multiplied by `SafeMultiplier` they are cancelled and recreated at
   fresh prices. This replaces the timer-based relocation logic from the MQL script.
7. **Lot sizing.** Either a fixed `ManualLotSize` is used or volume is derived from the
   portfolio equity and `RiskFactor`, mimicking the auto-lot calculation in the original
   code.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` / `ManualLotSize` | Base volume per order when automatic sizing is disabled. |
| `AutoLotSize`, `RiskFactor` | Enables equity-based lot sizing. |
| `UseVirtualLevels` | Keeps the stop-loss/take-profit logic on the strategy side. |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | Protection distances in pips. |
| `DelayModifyOrders`, `StepModifyOrders` | Control how quickly virtual levels are refreshed. |
| `PipsReplaceOrders`, `SafeMultiplier` | Force re-entry when pending orders are too far from the market. |
| `UsePendingOrders`, `DistancePending` | Switch between limit and market entries. |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | Trading window configuration. |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | Spread filter and reaction. |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | Spread averaging controls. |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | Broker stop level, execution slippage and the magic-number equivalent. |

All parameters are exposed through `StrategyParam<T>` so they are available in the
Designer UI and compatible with optimization runs.

## Differences vs. MQL
- StockSharp uses a netted position model; the strategy therefore cancels the opposite
  pending order once one side is filled to avoid flattening the net position. This still
  preserves the alternating hedge behaviour of the original EA.
- The `UseVirtualLevels` flag keeps stop-loss/target management within the strategy.
  The MQL expert relied on chart objects for visualisation; this port logs every update
  instead of drawing lines.
- Spread averaging is implemented as an incremental running mean, replacing the MQL
  array-based accumulator while honouring the same averaging period limit.

## High-level API usage
- `SubscribeLevel1().Bind(ProcessLevel1)` drives the whole decision engine based on
  best bid/ask updates.
- Entry and exit orders are created via `RegisterOrder`, `BuyMarket`, `SellMarket` style
  helpers, exactly as recommended in the conversion guidelines.
- `StartProtection()` is invoked once during `OnStarted`, matching the framework best
  practice for activating protective order support.
