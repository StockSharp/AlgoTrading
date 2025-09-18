# ENewsLuckyw Strategy

## Overview
The **ENewsLuckyw Strategy** is a time-based breakout system converted from the MetaTrader expert advisor *e-News-Lucky$*. At a scheduled time it submits buy-stop and sell-stop orders around the current price, continuously recenters them while both orders are active, and performs position management that mimics the original MQL logic. Protective exits, optional trailing, and an end-of-day cleanup complete the workflow.

## Trading Logic
- **Scheduled straddle placement.** At `SetOrdersTime` the strategy cancels any remaining pending orders, measures the current candle close, and places symmetric stop orders at `DistancePips` from the market price.
- **Continuous order refresh.** When both pending orders are active they are realigned on every finished candle, keeping the straddle centred on price like the original expert did on each new bar.
- **Entry preparation.** Stop-loss and optional take-profit levels are pre-calculated so they can be attached immediately when a position opens. Opposite pending orders are removed as soon as a position appears.
- **Trailing protection.** If `UseTrailing` is enabled, the stop order moves by `TrailingStopPips` whenever the position has advanced by at least `TrailingStepPips`. With `ProfitTrailing` turned on the trailing starts only after the profit exceeds the trailing distance, replicating the MQL "ProfitTrailing" switch.
- **Session cleanup.** At `DeleteOrdersTime` all pending orders are cancelled and any open position is closed to avoid holding risk overnight.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Order volume in lots used for both stop orders. |
| `StopLossPips` | Protective stop distance. Zero disables the stop. |
| `TakeProfitPips` | Optional take-profit distance. Zero disables the target. |
| `DistancePips` | Offset from the current price for the breakout stop orders. |
| `UseTrailing` | Enables stop trailing once the position is open. |
| `ProfitTrailing` | Requires unrealized profit to exceed the trailing distance before moving the stop. |
| `TrailingStopPips` | Distance between price and the trailing stop. |
| `TrailingStepPips` | Minimal improvement needed before the trailing stop is updated again. |
| `SetOrdersTime` | Time of day when the straddle is placed. |
| `DeleteOrdersTime` | Time of day for cancelling orders and closing positions. |
| `CandleType` | Candle subscription used for time tracking and order maintenance. |

## Usage Notes
1. Attach the strategy to the desired instrument and configure `CandleType` to match the bar size you want to use for maintenance (the default is 1-minute candles).
2. Set the schedule parameters to align with your news event or trading session.
3. Adjust distances and risk controls according to instrument volatility. For Forex symbols make sure the price step is configured correctly so that `StopLossPips`, `TakeProfitPips`, and `DistancePips` translate into the expected price offsets.
4. The trailing system uses stop and limit orders for exits. If your venue does not support these order types, replace them with market exits or simulated orders before going live.
5. The strategy performs a daily reset by date. If you run it across midnight in the exchange time zone, ensure the trading session spans within a single trading day.

## Conversion Notes
- The strategy mirrors the MQL expert's workflow: scheduled placement (`SetOrders`), hourly maintenance (`ModifyOrders`), removal of conflicting pending orders (`DeleteOppositeOrders`), trailing logic (`TrailingPositions`), and end-of-day cleanup.
- Spread-aware price calculations from the MQL code are approximated using the last candle close because StockSharp normalises prices to the instrument's `PriceStep`.
- All sound, account-number, and colour settings from the original script were omitted because they have no equivalent in StockSharp's high-level API.
