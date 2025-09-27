# E-Friday Session Strategy

## Overview
The E-Friday Session strategy replicates the classic MetaTrader expert advisor that trades only on Fridays. It observes the previous daily candle and opens a position at a configured hour at the start of Friday's session. The direction is contrarian: if the previous day closed below its open (bearish candle), the strategy buys; if the previous day closed above its open (bullish candle), the strategy sells. Positions are managed intraday and can be closed automatically after a configurable hour or by protective stops.

## Trading Rules
1. Collect daily candles (default: 1 day) to obtain the prior day's open and close.
2. On Fridays, monitor intraday candles (default: 1 minute) to detect the configured entry hour.
3. At the first candle of the entry hour:
   - Go long when the previous day was bearish.
   - Go short when the previous day was bullish.
   - Skip trading if the previous day was a doji (open equals close).
4. Optionally close the position automatically once the configured exit hour is reached.
5. Manage exits using stop-loss, take-profit, and optional trailing stop logic that mimics the original Expert Advisor, including the profit activation and trailing step thresholds.

## Implementation Notes
- Uses StockSharp high-level candle subscriptions for both daily context and intraday timing.
- Converts point-based risk controls from the MQL version into absolute price offsets using the security's price step.
- Maintains trailing stops in code, updating them on each finished candle and closing the position when price extremes are breached.
- Ensures only one trade per Friday by tracking daily state.
- Supports both long and short entries, respecting the original magic-number gating by trading a single symbol per strategy instance.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Trade size in lots/contracts. | `0.1` |
| `StopLossPoints` | Stop-loss distance in price steps (0 disables). | `75` |
| `TakeProfitPoints` | Take-profit distance in price steps (0 disables). | `0` |
| `HourOpen` | Hour of the day (0-23) to open the position. | `7` |
| `UseClosePositions` | Enable automatic closing after the exit hour. | `true` |
| `HourClose` | Hour of the day (0-23) to close the position if enabled. | `19` |
| `UseTrailing` | Enable trailing stop adjustments. | `true` |
| `ProfitTrailing` | Require profit to exceed trailing distance before trailing activates. | `true` |
| `TrailingStopPoints` | Trailing stop distance in price steps. | `60` |
| `TrailingStepPoints` | Additional points required before tightening the trailing stop. | `5` |
| `IntradayCandleType` | Candle type for intraday timing (default 1-minute candles). | `TimeSpan.FromMinutes(1)` |
| `DailyCandleType` | Candle type for daily sentiment detection (default 1-day candles). | `TimeSpan.FromDays(1)` |

## Usage Tips
- Align the instrument's trading session so that the Friday entry hour matches the desired market open.
- When configuring stop-loss and trailing values, express them in the same "points" used by the symbol's price step to reproduce the MetaTrader behavior.
- The strategy is designed for a single trade per Friday. To trade multiple symbols, run separate strategy instances per symbol.

## Differences from the Original EA
- Uses candle close data for decision making, whereas the original polled prices per tick.
- Protective exits are executed via market orders when candles indicate that stop or target levels were touched within the interval.
- Strategy parameters are exposed through StockSharp's `StrategyParam` system, supporting optimization and UI binding.
