# TakeProfitTimeGuardStrategy

## Overview

`TakeProfitTimeGuardStrategy` emulates the behaviour of the MetaTrader expert `Exp_GTakeProfit_Tm` by supervising the account-level profit and forcing a flat position state outside of a configurable trading schedule. The strategy does not open positions on its own. Instead, it serves as a risk-management overlay that automatically closes any existing exposure once the profit objective is achieved or when trading must halt outside the permitted time range.

## Core Logic

- Subscribes to a configurable candle stream (default 1-minute) to evaluate realised and unrealised PnL using the latest close price.
- Calculates **total profit** as the sum of realised PnL (`Strategy.PnL`) and the floating PnL derived from the current average position price.
- Ignores losses while the trading window is open, mirroring the original expert advisor behaviour.
- Once the **take-profit target** is reached, sets an internal stop flag and repeatedly liquidates any remaining positions until the account is flat. The stop flag resets after the portfolio returns to zero position.
- When the optional **trading window** is enabled, the strategy closes all positions whenever the current time falls outside the allowed range, also waiting until the book is flat before re-enabling trading.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | 1-minute time-frame | Candle series used to evaluate profit and schedule logic. |
| `TargetMode` | `ProfitTargetMode` (`Percent`/`Currency`) | `Percent` | Selects whether `TakeProfitValue` is interpreted as a percentage of account capital or as an absolute currency amount. |
| `TakeProfitValue` | `decimal` | `100` | Profit target threshold. Interpreted according to `TargetMode`. Must be greater than zero. |
| `UseTradingWindow` | `bool` | `true` | Enables or disables the time filter. |
| `StartTime` | `TimeSpan` | `00:00:00` | Beginning of the allowed trading window (inclusive). |
| `EndTime` | `TimeSpan` | `23:59:00` | End of the allowed trading window. When the start time is greater than the end time, the window spans midnight. |

## Behavioural Notes

1. The initial portfolio value is captured when the strategy starts (or on the first update if the value was zero) and is used as the reference for the percentage target.
2. The strategy computes floating PnL using the latest candle close price; results depend on the selected candle granularity.
3. If the profit target is met, the strategy keeps sending market orders to flatten the position until the book is empty. It logs the reason for closing the book.
4. When `UseTradingWindow` is enabled and the clock is outside the window, the same flattening routine executes even if the profit target was not reached.
5. The stop flag (`_stop`) clears only after the position returns to zero, allowing trading to resume when conditions permit.

## Differences from the Original MQL Strategy

- Uses StockSharp high-level API (`SubscribeCandles`) instead of per-tick handlers.
- Calculates floating profit from the average position price exposed by `Strategy.PositionPrice`.
- Logs take-profit events for easier monitoring.
- Time comparison is based on `DateTimeOffset.CloseTime` of the subscribed candles.

## Usage Tips

- Attach the strategy to a portfolio already running another trading strategy to act as a guard layer.
- Choose a candle timeframe that matches the responsiveness required for profit evaluation (e.g., 1-minute for rapid control).
- Ensure the portfolio information (especially `CurrentValue`) is available; otherwise, set an explicit starting balance before running percentage targets.
- The strategy can be combined with `StartProtection()` in another primary strategy to add further risk controls.
