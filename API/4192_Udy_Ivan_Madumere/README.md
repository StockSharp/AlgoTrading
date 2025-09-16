# Udy Ivan Madumere Strategy

## Overview
The Udy Ivan Madumere expert advisor opens a single market position once per day when a specific hourly candle appears. The StockSharp port keeps this behaviour intact by watching the configured candle series, comparing historic open prices, and reacting immediately after the target bar closes. All execution decisions, position management, and volume handling are reproduced so that the strategy behaves like the MetaTrader 4 original inside the StockSharp environment.

Key characteristics:

- Evaluates one finished candle per day at `TradeHour` and never submits more than one concurrent position.
- Measures the difference between the open prices `Open[FirstLookback]` and `Open[SecondLookback]` to decide whether to short or go long.
- Mirrors the MetaTrader balance ladder to adjust the base lot size automatically when `UseAutoVolume` is enabled.
- Applies asymmetric stop-loss and take-profit distances (separate for long and short) and a trailing stop that only affects short positions.
- Forces every trade to close after a configurable number of hours even if protective levels were not hit.

## Trading workflow
1. Subscribe to the selected candle type (`CandleType`) and wait for fully finished bars to prevent premature signals.
2. Track the open price history so the differences `Open[FirstLookback] - Open[SecondLookback]` (short setup) and `Open[SecondLookback] - Open[FirstLookback]` (long setup) can be evaluated exactly like in MetaTrader.
3. When the most recent candle opens at `TradeHour`:
   - If the bearish difference is larger than `ShortDeltaPoints * PriceStep`, send a market sell order.
   - Otherwise, if the bullish difference exceeds `LongDeltaPoints * PriceStep`, send a market buy order.
4. Only one order is allowed per day. The `canTrade` flag is reset after the configured hour has passed to allow another attempt on the next session.
5. Upon order entry the strategy recalculates the base lot:
   - `UseAutoVolume = true` activates the legacy ladder that increases the lot size when the account balance crosses predefined thresholds.
   - If the current balance is below the snapshot from the previous trade, the result is multiplied by `BigLotMultiplier`, matching the “big lot” recovery behaviour of the EA.
6. While the position is open the following exit logic runs on each completed candle:
   - Hard take-profit and stop-loss are evaluated against the recorded entry price.
   - Short trades also trail the stop once the best price has improved by at least `TrailingStopPoints`.
   - The position is closed forcefully once it has been alive for `MaxHoldingHours`.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H1` | Candle series processed by the strategy. |
| `TradeHour` | `int` | `18` | Hour of the day (0-23) when the daily signal is evaluated. |
| `FirstLookback` | `int` | `6` | Number of completed candles referenced as `Open[FirstLookback]`. |
| `SecondLookback` | `int` | `2` | Number of completed candles referenced as `Open[SecondLookback]`. |
| `LongDeltaPoints` | `decimal` | `6` | Minimum bullish open-price difference (in MetaTrader points) required to enter a long. |
| `ShortDeltaPoints` | `decimal` | `21` | Minimum bearish open-price difference (in MetaTrader points) required to enter a short. |
| `TakeProfitLongPoints` | `decimal` | `39` | Take-profit distance, expressed in points, for long positions. |
| `StopLossLongPoints` | `decimal` | `147` | Stop-loss distance, in points, for long positions. |
| `TakeProfitShortPoints` | `decimal` | `200` | Take-profit distance, in points, for short positions. |
| `StopLossShortPoints` | `decimal` | `267` | Stop-loss distance, in points, for short positions. |
| `TrailingStopPoints` | `decimal` | `30` | Trailing-stop distance (points) applied only to short positions. |
| `BaseVolume` | `decimal` | `0.01` | Initial lot size before money-management adjustments. |
| `UseAutoVolume` | `bool` | `true` | Enable the MetaTrader balance ladder that overrides `BaseVolume`. |
| `BigLotMultiplier` | `decimal` | `1` | Extra multiplier applied when the balance dropped since the previous trade. |
| `MaxHoldingHours` | `int` | `504` | Maximum holding time in hours. Zero disables the timer. |

## Implementation notes
- Price thresholds are converted from MetaTrader “points” into actual price distances using the instrument’s `PriceStep`.
- The open-price buffer is trimmed to `max(FirstLookback, SecondLookback) + 1` entries, avoiding unnecessary allocations while keeping the required history.
- The trailing stop for short trades stores the best achieved low and updates the protective level only when the new candidate is closer to the current price.
- Account balance snapshots rely on `Portfolio.CurrentValue` (falling back to `BeginValue`) so that demo, live, and backtest environments behave consistently.
- Every comment inside the code is written in English as requested, making the logic easy to audit or extend.

## Usage tips
- Match `CandleType` with the timeframe used by the historical EA (the original template expects one-hour candles).
- When running on symbols that use micro lots, adjust `BaseVolume` and the auto-lot ladder values to the venue’s contract specifications.
- Combine the strategy with StockSharp charts via the built-in helpers (`DrawCandles`, `DrawOwnTrades`) to verify that orders appear only once per day at the configured hour.
