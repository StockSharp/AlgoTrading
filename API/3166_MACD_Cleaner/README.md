# MACD Cleaner Strategy

## Overview
The **MACD Cleaner** strategy is a conversion of the "MACD Cleaner" MetaTrader 5 expert advisor. It analyses completed candles from a single timeframe and places trades when the MACD main line increases or decreases monotonically during three consecutive closed bars. The system always keeps at most one directional position and flips when the momentum reverses.

## Trading Logic
- On every finished candle the strategy reads the MACD line calculated with the configured fast, slow, and signal periods.
- If the last three MACD values are non-decreasing, the strategy prepares a long entry. If a short position exists it is closed first, then a new long position is opened.
- If the last three MACD values are non-increasing, the strategy prepares a short entry. Existing long positions are flattened before opening the short.
- Protective stop-loss and take-profit levels are evaluated on candle highs and lows using the pip-based offsets.
- When trailing parameters are enabled the stop is pulled in the trade direction once the price progresses by at least the configured trailing step.
- All exit orders are issued as market orders using the aggregated position volume to ensure the entire position is closed.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | 1 hour candles | Timeframe used for MACD calculation and order evaluation. |
| `TradeVolume` | 1 | Base volume submitted for a new position. If the opposite side is open the absolute position volume is added to close it before reversing. |
| `StopLossPips` | 35 | Stop-loss distance in pips from the entry price. Set to zero to disable the stop. |
| `TakeProfitPips` | 30 | Take-profit distance in pips from the entry price. Set to zero to disable the target. |
| `TrailingStopPips` | 0 | Trailing stop distance. When zero the trailing logic is disabled. |
| `TrailingStepPips` | 5 | Minimum favourable move (in pips) required before the trailing stop is adjusted. Ignored when the trailing stop is disabled. |
| `MacdFastPeriod` | 15 | Fast EMA length for the MACD indicator. |
| `MacdSlowPeriod` | 33 | Slow EMA length for the MACD indicator. |
| `MacdSignalPeriod` | 11 | Signal EMA length for the MACD indicator. |

## Order Management
- Long exits: the strategy issues a market sell order when the stop-loss, take-profit, or trailing level is hit.
- Short exits: a market buy order closes the position under the same conditions, mirrored for short trades.
- After the position is fully closed the trailing state is reset so that the next trade starts with fresh levels.

## Notes
- Pip size is automatically derived from the instrument. For symbols with 3 or 5 decimal places the pip equals ten minimal price steps, mimicking the original MetaTrader implementation.
- The logic only evaluates completed candles and does not act on intrabar changes.
- To disable risk management set the corresponding pip distances to zero. Trailing requires both `TrailingStopPips` and a positive `TrailingStepPips` to work.
