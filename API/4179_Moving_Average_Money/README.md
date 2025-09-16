# Moving Average Money Strategy

## Overview
The strategy is a StockSharp conversion of the MetaTrader expert advisor "Moving Average Money". It evaluates completed candles and reacts when the previous bar crosses a shifted simple moving average. The system supports both long and short trades and keeps every decision synchronized with the high-level candle subscription API.

## Trading Logic
- A simple moving average with configurable length and visual shift is calculated from close prices.
- Only finished candles are processed to prevent duplicate orders within a bar.
- **Short entry:** when the previous candle opens above the shifted moving average and closes below it.
- **Long entry:** when the previous candle opens below the shifted moving average and closes above it.
- The strategy does not pyramid positions; any open exposure in the opposite direction is closed before establishing a new trade.

## Risk Management
- The stop-loss distance in price units is derived from `MaximumRiskPercent`. The current portfolio value, the instrument price step and the step price are used to convert the chosen risk percentage into price steps.
- The bid/ask spread is subtracted from the risk-based distance whenever best quotes are available.
- Take-profit levels are defined as `stopDistance * ProfitLossFactor`.
- Both stop and target levels are monitored on completed candles. When either level is reached the position is flattened with a market order.

## Parameters
- `CandleType` – time frame used for signal detection.
- `MovingPeriod` – length of the simple moving average.
- `MovingShift` – number of fully formed candles used to shift the moving average to the right.
- `MaximumRiskPercent` – percentage of the current portfolio value that defines the maximum loss per trade.
- `ProfitLossFactor` – multiplier applied to the stop distance to compute the take-profit distance.
- `TradeVolume` – base order volume for new entries (volume step constraints are respected automatically).

## Implementation Notes
- The strategy keeps track of open positions via high-level event handlers (`OnOwnTradeReceived`) to reinitialise stops and targets after fills.
- If market data lacks quotes or portfolio valuation, new entries are skipped to avoid orders without proper risk control.
- The moving average shift is emulated with an internal buffer so that the logic matches the MetaTrader version.
