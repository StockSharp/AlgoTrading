# EMA Cross Contest Hedged

## Overview
- Recreates the MetaTrader strategy "EMA Cross Contest Hedged" using StockSharp high level API.
- Trades a pair of exponential moving averages (EMA) and optionally confirms with the MACD main line.
- Builds a ladder of pending stop orders ("hedge" levels) after each entry to scale into strong trends.
- Applies static stop-loss/take-profit levels expressed in pips and a trailing stop that activates after a minimum gain.
- Allows choosing whether signals should use the current completed candle or the previous closed candle.

## Indicators and data
- Short EMA with configurable length (default 4).
- Long EMA with configurable length (default 24); the short period must stay below the long period.
- MACD (4, 24, 12) main line used as an optional confirmation filter.
- Works on any timeframe provided by the `CandleType` parameter (default 15-minute candles).

## Entry logic
1. Wait for a finished candle from the configured timeframe.
2. Calculate the fast and slow EMA values. Depending on `TradeBar`, determine the crossover using either:
   - The latest and previous finished candle (`Current`).
   - The previous and the candle before it (`Previous`, default).
3. Generate a long signal when the fast EMA crosses above the slow EMA. If `UseMacdFilter` is enabled the MACD value for the same bar must be non-negative.
4. Generate a short signal when the fast EMA crosses below the slow EMA. With the MACD filter enabled the MACD value must be non-positive.
5. Only open a new position when no exposure is present (all previous trades are flat).
6. Execute market orders with size `OrderVolume`. After an entry the strategy:
   - Stores stop-loss and take-profit levels offset by `StopLossPips` and `TakeProfitPips` from the fill price.
   - Resets the trailing-stop state.
   - Creates four hedging stop orders spaced by `HedgeLevelPips` in the trade direction. Each pending order inherits the same stop-loss/take-profit distance and expires after `PendingExpirationSeconds` seconds unless the price reaches it earlier.

## Exit management
- **Stop-loss / take-profit:** The strategy monitors intrabar highs and lows. If price touches the stored stop or target the whole position is closed.
- **Trailing stop:** When profit exceeds `TrailingStopPips + TrailingStepPips` the stop is trailed to `TrailingStopPips` behind the latest close. Long positions trail upward, short positions trail downward.
- **Opposite crossover:** When `CloseOppositePositions` is enabled the position is closed as soon as the opposite EMA crossover is detected.
- **Pending ladder:** Each hedging order turns into an additional market order once price crosses the stop level. New fills adjust the average entry price and tighten protective levels accordingly.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Order size used for every market or stop order. |
| `StopLossPips` | 140 | Stop distance in pips. Set to 0 to disable. |
| `TakeProfitPips` | 120 | Take-profit distance in pips. Set to 0 to disable. |
| `TrailingStopPips` | 30 | Trailing stop distance in pips. Set to 0 to disable trailing. |
| `TrailingStepPips` | 1 | Minimum additional profit (in pips) before the trailing stop tightens again. |
| `HedgeLevelPips` | 6 | Distance between the staged hedging stop orders. |
| `CloseOppositePositions` | false | Close the active position when an opposite crossover appears. |
| `UseMacdFilter` | false | Require MACD confirmation (>= 0 for longs, <= 0 for shorts). |
| `PendingExpirationSeconds` | 65535 | Lifetime of each hedging stop order in seconds. |
| `ShortMaPeriod` | 4 | Short EMA length. Must be less than `LongMaPeriod`. |
| `LongMaPeriod` | 24 | Long EMA length. |
| `TradeBar` | Previous | Determines which bar pair is used to detect the crossover. |
| `CandleType` | 15-minute | Timeframe requested from the data provider. |

## Additional notes
- Pips are converted by multiplying `Security.PriceStep` and automatically applying a factor of 10 for 3- and 5-decimal instruments to match MetaTrader pip conventions.
- Pending hedging orders are simulated inside the strategy and execute as soon as the candle range touches their level.
- `StartProtection()` is invoked to activate the built-in StockSharp position-protection services.
- The strategy keeps separate trailing-stop logic for long and short positions to mirror the original hedged implementation.
