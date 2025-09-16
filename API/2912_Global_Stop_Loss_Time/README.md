# Global Stop Loss & Trading Window Strategy

## Overview
This strategy replicates the behaviour of the MetaTrader expert **Exp_GStopLoss_Tm**, providing a risk overlay that monitors the
combined result of all trades opened by the strategy instance. The module does not generate entry signals by itself; instead it
tracks the profit and loss of existing positions and enforces both a global stop loss threshold and an optional trading session
window. When losses exceed the configured limit or the market moves outside the permitted time range, the strategy liquidates the
current exposure and blocks any further trades until the book is flat again.

## Trading Logic
1. On start-up the strategy records the current realised PnL as the baseline reference. This allows it to measure floating profit
   relative to the most recent flat state.
2. Each finished candle produced by the configured candle type triggers a risk check. The default timeframe is one minute to
   emulate tick-level surveillance without overwhelming the system.
3. The module calculates the unrealised profit as the difference between the current strategy PnL and the baseline value. Positive
   PnL is ignored while the strategy remains inside the trading window, matching the original expert advisor.
4. If the loss mode is set to **Percent**, the strategy compares the absolute loss percentage against the account equity obtained
   from `Portfolio.CurrentValue`. For **Currency** mode the comparison is made in absolute currency units.
5. Once the loss threshold is exceeded the stop flag is latched and the strategy begins closing the open position on the next
   iteration. The flag is released only after the position size returns to zero and the baseline PnL is refreshed.
6. When the optional trading window is enabled, the risk check also evaluates whether the candle close time sits inside the allowed
   interval. The window supports intraday sessions that wrap around midnight, mirroring the MetaTrader logic.
7. Any time the stop flag is active or the session filter detects that the market is outside the permitted hours, the module sends a
   market order in the opposite direction to flatten the position. Informational log entries describe the reason for each exit.

## Parameters
| Name | Description |
| ---- | ----------- |
| `LossMode` | Selects how the loss threshold is interpreted: percentage of current account equity or absolute account currency. |
| `StopLoss` | Loss threshold value. For percentage mode the number represents percent, while currency mode uses the account currency. |
| `UseTimeFilter` | Enables the intraday trading window. When disabled the strategy ignores the time filter entirely. |
| `StartTime` | Inclusive start of the trading window in UTC. Works together with `EndTime` to define the valid session. |
| `EndTime` | Exclusive end of the trading window in UTC. Supports wrap-around sessions when the end time is earlier than the start. |
| `CandleType` | Candle subscription used to drive the periodic risk evaluation. The default is a 1-minute time frame. |

## Implementation Notes
- The baseline PnL is recalculated whenever the position size returns to zero so that subsequent trades start with a clean slate.
- Equity values are pulled from the live portfolio, therefore the percentage mode adapts to both realised and unrealised changes in
  account value.
- All comments in the source code are written in English as required by the project conventions.
- The strategy draws candles and own trades on the default chart area when one is available, helping to visualise the behaviour
  during testing.

## Usage Guidelines
1. Attach the strategy to the instrument you want to supervise. Order generation from other strategies can still occur; this
   module only monitors and closes positions.
2. Configure the loss mode and threshold that match your risk appetite. For example, `LossMode = Percent` and `StopLoss = 5` will
   close the position after a 5% unrealised drawdown relative to current equity.
3. Set the `StartTime` and `EndTime` parameters to limit trading to a particular intraday session. To cover an overnight window,
   specify a start time later than the end time (for example 20:00 to 06:00).
4. Run the backtest or live session. The strategy will automatically reset the stop flag once all positions are flattened and will
   continue to supervise subsequent trades.
