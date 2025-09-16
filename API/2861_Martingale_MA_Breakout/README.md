# Martingale MA Breakout Strategy (ID 2861)

## Overview

The Martingale MA Breakout strategy is a port of the original MetaTrader 5 expert advisor `Martingale.mq5`. It monitors how far the current price moves away from a moving average plotted on a higher timeframe. When the distance exceeds a configurable number of pips, the strategy opens a new position in the direction of the move and manages it with fixed stop-loss, take-profit, and trailing logic. Position sizing follows a martingale-style adjustment that increases the trade size after losing sequences and reduces it after profitable periods.

By default the strategy evaluates 6-minute candles while the surrounding platform can operate on any base timeframe. All indicator calculations are performed on the selected candle type, while orders are sent using market execution.

## Trading Logic

1. Calculate the moving average value for the current candle using the selected smoothing method, applied price, and shift.
2. Transform the configured pip distance into an absolute price delta. The pip size replicates the original MQL tuning: symbols with 3 or 5 decimal digits multiply the price step by 10.
3. When the candle closes:
   - If the close is more than `DistanceFromMaPips` pips above the shifted moving average and there is no active long exposure, send a market buy order.
   - If the close is more than `DistanceFromMaPips` pips below the shifted moving average and there is no active short exposure, send a market sell order.
4. Every finished candle also updates the trailing stop and checks whether the close price breaches the simulated stop-loss or take-profit. Closing a position triggers `ResetTradeState`, clearing all stored levels.

## Money Management

- `RiskPercent` converts into a monetary risk budget using `Portfolio.CurrentValue` (or `BeginValue` if no trades were made). When a stop-loss is specified, the budget divided by stop distance and security multiplier estimates the maximum affordable volume.
- After sizing by risk, the volume passes through `ApplyMartingale`: if the last recorded balance (captured after the previous entry) is higher than the current balance, the volume increases by 1 unit; if it is lower, the volume decreases by 1 unit but never drops below the base strategy volume.
- Trailing logic mimics the original EA: once price moves by `TrailingStopPips + TrailingStepPips` in favor of the position, the stop is pulled to maintain the `TrailingStopPips` offset. The strategy validates that `TrailingStepPips` is non-zero when trailing is enabled, mirroring the MQL error handling.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StopLossPips` | Stop-loss distance expressed in pips. A value of zero disables the stop and risk-based sizing. |
| `TakeProfitPips` | Take-profit distance in pips. Set to zero to disable. |
| `TrailingStopPips` | Trailing stop offset in pips. Must be paired with `TrailingStepPips`. |
| `TrailingStepPips` | Additional price move required before the trailing stop advances. Cannot be zero when trailing is active. |
| `DistanceFromMaPips` | Minimal distance between price and shifted moving average that triggers entries. |
| `CandleType` | Data type used for indicator calculations (defaults to 6-minute time frame). |
| `MaPeriod` | Moving average period. |
| `MaShift` | Number of bars the moving average is shifted forward. The strategy stores historical MA values to emulate the MQL behaviour. |
| `MaMethod` | Moving average smoothing type: Simple, Exponential, Smoothed, or Weighted. |
| `MaAppliedPrice` | Candle price used for the moving average (close, open, high, low, median, typical, or weighted). |
| `RiskPercent` | Percentage of current equity allocated to the stop-loss risk budget. |

## Execution Notes

- The strategy works exclusively on finished candles to replicate the “new bar” processing of the original EA. `BuyMarket`/`SellMarket` will flip existing exposure by adding the absolute value of the opposite position.
- Stops and targets are simulated in-code because StockSharp does not automatically manage them in this conversion. The close price is used as a proxy for tick-level execution.
- Martingale adjustments operate on the account balance snapshot taken immediately after each entry, similar to the source EA.
- If the symbol lacks a valid price step or multiplier, fallback defaults of `0.0001` and `1` are used to avoid division errors.

## Differences from the Original EA

- The MQL version used bid/ask prices; this port works with candle close prices because high-frequency ticks are not available in the high-level API.
- Volume sizing relies on portfolio equity and security multiplier instead of the `CMoneyFixedMargin` helper.
- Chart visualisation is optional: when a chart area is available, the strategy draws candles; no extra indicators are plotted by default.
- The validation that `TrailingStepPips` must be positive when trailing is enabled throws an exception during start-up instead of calling `Alert`.

