# Candle Shadows V1 Strategy

## Overview
Candle Shadows V1 is a price action reversal strategy that recreates the original MetaTrader expert advisor logic inside the StockSharp high-level API. The system looks for candles with a strong dominant wick and minimal opposite shadow during a configurable trading session. Trades are only allowed during the first minutes of a bar, emulating the intrabar execution of the MQL version while still working on closed candles.

## Trading logic
1. Subscribe to the configured time-frame candles (default 5 minutes) and evaluate only finished bars.
2. Enforce a session window using the `StartHour` and `EndHour` parameters. If the candle opens outside the window no trade is considered.
3. Allow entries only if the candle closes before `OpenWithinMinutes` from its open time, preventing late signals on long bars.
4. Long setup: the candle must print a lower shadow greater than `CandleSizeMinPips` pips and the upper shadow must stay within `OppositeShadowMaxPips` pips. When the conditions are satisfied and there is no open position a market buy is sent.
5. Short setup: the candle must print an upper shadow greater than `CandleSizeMinPips` pips and the lower shadow must stay within `OppositeShadowMaxPips` pips. A market sell is issued if the account is flat.
6. Only one trade per candle is permitted, matching the original “one order per bar” constraint.

## Position management
- Initial protective distances are expressed in pips and converted through the `PipValue` parameter for every instrument.
- Hard stop-loss and take-profit checks are performed on every finished candle. If the candle’s high/low touches the threshold the position is flattened.
- Trailing management mimics the MQL trailing stop: once price advances by at least `TrailingStopPips + TrailingStepPips` the stop is moved in increments of `TrailingStepPips` pips.
- If a position stays open longer than `PositionLivesBars` bars it is closed immediately. Profitable trades are also forced out after `CloseProfitsOnBar` bars to lock in gains.
- The next trade volume is reduced by dividing the `BaseVolume` by `LossReductionFactor` whenever the previous trade closed with a loss, just like the lot reduction in the original expert advisor.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `PipValue` | Monetary value of one pip used to transform pip distances into price offsets. | `0.0001` |
| `StopLossPips` | Stop-loss distance in pips. Set to `0` to disable the hard stop. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. Set to `0` to disable the hard target. | `50` |
| `TrailingStopPips` | Trailing stop distance in pips. When `0` no trailing is applied. | `15` |
| `TrailingStepPips` | Minimum step in pips between trailing stop adjustments. Must be positive when trailing is enabled. | `5` |
| `PositionLivesBars` | Maximum number of completed bars a position can remain open before it is force-closed. | `4` |
| `CloseProfitsOnBar` | When greater than zero, profitable positions are closed after this many bars from entry. | `2` |
| `OpenWithinMinutes` | Maximum amount of minutes after bar open when new trades are allowed. | `7` |
| `CandleSizeMinPips` | Required wick length (in pips) on the dominant side of the candle. | `15` |
| `OppositeShadowMaxPips` | Maximum size (in pips) of the opposite candle shadow. | `1` |
| `StartHour` | Session start hour in exchange time (0–23). | `6` |
| `EndHour` | Session end hour in exchange time (0–23). | `18` |
| `LossReductionFactor` | Divisor applied to `BaseVolume` after a losing trade. | `1.5` |
| `BaseVolume` | Default market order size used for entries. | `1` |
| `CandleType` | Candle series used for the calculations. Default is a 5-minute time frame. | `5 min` |

## Notes
- Always adjust `PipValue` to match the instrument’s tick size (for example `0.01` for JPY crosses or `1` for index futures).
- Because the strategy works with completed candles, executions will happen at bar close. Lower time frames (1–5 minutes) best replicate the intrabar behavior of the original expert advisor.
- No external indicators are required, which makes the strategy easy to run on any StockSharp data source.
