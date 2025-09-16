# Previous Candle Breakdown Strategy

## Overview
This strategy reproduces the "Previous Candle Breakdown" MetaTrader expert advisor. It waits for the price to break above or below the previous reference candle with a configurable indent measured in price steps. The implementation relies on high-level StockSharp APIs with candle subscriptions for level calculations and tick subscriptions for execution decisions.

## Trading Logic
1. At the close of every reference candle (default 4 hours) the strategy stores the previous candle high and low and offsets them by `IndentSteps * Security.PriceStep` to build breakout levels.
2. Tick prices (last trades) are monitored. A long entry is triggered when price reaches the upper level and a short entry when price falls through the lower level.
3. An optional moving-average filter requires the fast MA (with optional forward shift) to stay above the slow MA for long trades and below it for short trades. Setting either MA period to zero disables the filter.
4. Trades are allowed only inside the configured session window between `StartTime` and `EndTime`. Crossing-midnight sessions are supported.
5. Floating profit is monitored continuously: stops, targets and trailing rules close existing positions before a breakout signal can trigger reversals.

## Risk Management
- **StopLossSteps / TakeProfitSteps** — distances in price steps from the entry price. Steps are converted via `distance = steps * Security.PriceStep`.
- **TrailingStopSteps / TrailingStepSteps** — enables a trailing exit once the position moves in favor by at least the trailing distance. The stop is moved further only when profit advances by the trailing step.
- **ProfitClose** — closes all positions once the unrealized profit (`Position * (last price - PositionPrice)`) exceeds the threshold. Set to `0` to disable.
- **MaxNetPosition** — caps the absolute net position so the strategy cannot pyramid beyond that amount. Position size itself is controlled by the strategy `Volume` property.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Reference timeframe used to calculate breakout levels. |
| `IndentSteps` | Offset above/below the previous candle high/low expressed in price steps. |
| `FastMaPeriod` / `FastMaShift` | Fast moving average length and optional forward shift (bars). |
| `SlowMaPeriod` / `SlowMaShift` | Slow moving average length and optional forward shift (bars). |
| `StopLossSteps` | Stop loss distance in price steps. |
| `TakeProfitSteps` | Take profit distance in price steps. |
| `TrailingStopSteps` | Trailing stop distance (0 disables trailing). |
| `TrailingStepSteps` | Minimal gain required before the trailing stop is advanced. Must be > 0 when trailing is used. |
| `ProfitClose` | Floating profit target that closes all positions. |
| `MaxNetPosition` | Maximum absolute net position allowed. |
| `StartTime` / `EndTime` | Trading window boundaries. |

## Usage Notes
- Set the `Volume` property of the strategy instance to control order size. Risk-based position sizing from the MetaTrader version is intentionally not ported.
- The moving averages use simple moving averages (`SMA`). If other smoothing modes are required, extend the strategy accordingly.
- The profit close threshold uses unrealized profit in instrument price units (quantity × price difference). Adjust the threshold so it matches your instrument.
- The strategy operates in a netting environment; reversing trades send market orders in the opposite direction, automatically closing the current exposure first.
- Trailing stop requires a positive `TrailingStepSteps` value; otherwise the strategy throws an exception during start-up.

## Differences from the Original MQL Version
- Money management based on fixed lots or risk percentage is not implemented; StockSharp users should manage size via the `Volume` property or external portfolio managers.
- Only simple moving averages are supported; the original allowed different MA types.
- Profit-close logic uses floating PnL computed from average position price instead of account currency, because brokerage-specific swap/commission data are not directly available.
- Logging is handled by StockSharp; detailed trade result messages from MetaTrader are omitted.
