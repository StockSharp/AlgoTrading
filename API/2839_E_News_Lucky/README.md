# E-News Lucky Strategy

## Overview
The **E-News Lucky Strategy** is a StockSharp port of the MetaTrader expert advisor `e-News-Lucky`. The system automates the classic news breakout approach:

- At a configurable `PlacementTime` it submits both buy-stop and sell-stop orders around the current price, offset by `DistancePips`.
- When either pending order is executed, the opposite order is cancelled immediately. Initial protective stop-loss and take-profit levels are attached according to the configured pip offsets.
- A trailing stop can be enabled via `TrailingStopPips` and `TrailingStepPips` to lock in profits as the trade moves in the favourable direction.
- At the configured `CancelTime` all remaining pending orders are removed and any open positions are closed to avoid holding risk outside the trading window.

The strategy uses candle data (`CandleType`, 1-minute by default) only to track the scheduled times and to update the trailing stop. It does not rely on indicator calculations.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Order volume for each pending entry. The strategy sends symmetric buy-stop and sell-stop orders with this volume. |
| `StopLossPips` | Distance between the entry price and the protective stop-loss, expressed in pips. Set to zero to disable the stop. |
| `TakeProfitPips` | Distance between the entry price and the profit target in pips. Set to zero to disable the target. |
| `TrailingStopPips` | Trailing stop distance in pips. The trailing engine becomes active only when this value is greater than zero. |
| `TrailingStepPips` | Minimum pip gain required before the trailing stop is moved again. Prevents excessive stop updates in ranging markets. |
| `DistancePips` | Offset (in pips) from the current price used to place the stop orders. |
| `PlacementTime` | Time of day (broker/server time) when the pending orders are placed. Default: 10:30. |
| `CancelTime` | Time of day when pending orders are cancelled and open positions are closed. Default: 22:30. |
| `CandleType` | Candle series used for scheduling and trailing. Default: 1-minute time frame. |

## Implementation Notes
- Pip size follows the MetaTrader logic: if the symbol has 3 or 5 digits, the strategy multiplies the price step by 10 to work in pip units.
- All prices are normalized to the instrument price step before orders are submitted.
- Trailing stops compare the latest close against `PositionPrice` and only move the protective stop when the gain exceeds both `TrailingStopPips` and `TrailingStepPips`.
- Pending orders are recreated each trading day when the placement time is reached. Cancel time checks ensure all exposure is flat by the end of the window.

## Usage Tips
1. Attach the strategy to a liquid instrument with tight spreads; the breakout distances assume news-like price behaviour.
2. Set `PlacementTime` and `CancelTime` according to the economic calendar of interest.
3. Adjust pip distances to match the instrument volatility. Larger values reduce the chance of false triggers, while smaller values can capture earlier moves but increase whipsaw risk.
4. Disable trailing by keeping `TrailingStopPips` at zero if fixed stops are preferred.
5. Monitor slippage and spread during high-impact news to ensure the pending orders are filled as expected.
