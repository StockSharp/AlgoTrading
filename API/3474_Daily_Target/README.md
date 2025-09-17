# Daily Target Strategy

## Overview

`DailyTargetStrategy` replicates the MetaTrader 4 expert advisor "Daily Target". The strategy keeps trading open positions until
the combined profit and loss for the current calendar day reaches a configured profit target or breaches a maximum loss limit. As
soon as either threshold is hit, all active orders are cancelled and the position is flattened so trading remains paused until the
next day begins.

## Trading Logic

1. **Start-up**
   - The strategy calls `ResetDailySnapshot` during `OnStarted` to store the current date and the realized PnL baseline.
   - `SubscribeLevel1()` delivers bid/ask updates which are required to evaluate floating profit accurately.
   - `SubscribeTrades()` captures the last executed price, providing a fallback when quotes are missing.
   - A one-minute `Timer` tick ensures that date changes are detected even when no market data arrives.
2. **PnL evaluation**
   - `EvaluateDailyThresholds` recomputes the realized PnL (current `PnL` minus the stored baseline) and adds the floating PnL
     calculated from the latest bid/ask or last trade price.
   - If the total daily PnL crosses the configured target or drops below the negative loss limit, the strategy calls
     `TriggerDailyStop`.
3. **Emergency exit**
   - `TriggerDailyStop` writes an informational log entry, cancels all pending orders, and sends the appropriate market order to
     flatten the remaining long or short exposure.
   - `_dailyStopTriggered` prevents re-entry during the same day. When the calendar date changes, `ResetDailySnapshot` clears this
     flag and records a new PnL baseline.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `DailyTarget` | `10` | Profit target in portfolio currency. Trading stops for the rest of the day once the total daily PnL meets or exceeds this value. |
| `DailyMaxLoss` | `0` | Maximum tolerated loss in portfolio currency. Set to zero to disable the loss filter. Trading is halted for the day once the total daily PnL drops below the negative threshold. |

## Notes

- The strategy only manages the primary `Security` assigned to the strategy instance, mirroring the single-symbol behaviour of the
  MQL expert.
- Floating PnL uses the best bid for long positions and the best ask for short positions. If no quote is available, the last trade
  price acts as a fallback to avoid stalling the evaluation.
- No Python port is provided; only the C# high-level implementation is included in this package.
