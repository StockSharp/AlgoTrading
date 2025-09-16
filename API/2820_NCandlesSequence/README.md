# N-Candles Sequence Strategy

## Overview
The N-Candles Sequence strategy replicates the behaviour of the original MetaTrader expert "N-_Candles_v7" using the StockSharp high-level API. It monitors finished candles and looks for a configurable number of consecutive bullish or bearish bodies. When a qualifying streak is present, the strategy opens a position in the same direction and manages it with configurable take profit, stop loss, trailing stop, trading hours filter, and floating profit lock.

## Trading Logic
- Evaluate each finished candle and classify it as bullish, bearish, or neutral (doji). Neutral candles reset the streak counter and can trigger the "black sheep" behaviour.
- Maintain a running count of consecutive candles with the same body direction. Once the count reaches the configured threshold, the current direction becomes the active pattern.
- When a bullish streak is active the strategy attempts to open a long position; when a bearish streak is active it attempts to open a short position. Only one net position is held at a time.
- If a candle breaks the uniform direction ("black sheep"), the strategy reacts according to the selected closing mode: close everything, close only opposite positions, or close only positions aligned with the previous streak.
- Optionally restrict entries to a trading window defined by start and end hours (inclusive).
- Continuously monitor the open position for take profit, stop loss, trailing stop updates, and the floating profit threshold.

## Position and Risk Management
- The initial protective stop and target are calculated from pip distances converted with the instrument price step. These levels are recalculated each time a new position is opened.
- Trailing stop logic mimics the original expert: once price travels by the trailing distance plus step, the stop is moved to maintain the trailing distance.
- A floating profit guard (`MinProfit`) closes the entire position once the open profit exceeds the configured value.
- The `MaxPositionVolume` parameter prevents entries if the requested volume is above the allowed limit. `MaxPositions` works as a guard against re-entry when a position is already active.
- All exits call market orders to flatten the net position because the StockSharp strategy operates in a netting environment.

## Parameters
| Name | Description |
| --- | --- |
| `ConsecutiveCandles` | Number of candles with identical direction required to trigger a signal. |
| `OrderVolume` | Market order volume used for entries and exits. |
| `TakeProfitPips` | Take profit distance expressed in pips. Set to zero to disable. |
| `StopLossPips` | Stop loss distance expressed in pips. Set to zero to disable. |
| `TrailingStopPips` | Trailing stop distance. Set to zero to disable trailing. |
| `TrailingStepPips` | Additional distance required before the trailing stop is moved. |
| `MaxPositions` | Maximum number of simultaneous entries per pattern (the strategy keeps a single net position). |
| `MaxPositionVolume` | Upper bound for the allowed net volume. |
| `UseTradeHours` / `StartHour` / `EndHour` | Enable and configure the trading time window (inclusive). |
| `MinProfit` | Floating profit threshold that triggers a full exit. |
| `ClosingBehavior` | Defines how to react when a "black sheep" candle appears. |
| `CandleType` | The candle series used for calculations. |

## Notes and Assumptions
- The strategy operates with net positions; hedging-style multiple tickets are not created. This differs from the original expert where several hedged positions could coexist.
- Floating profit is approximated as `(current price - entry price) * volume` for long positions and the inverse for short positions.
- The pip conversion relies on the instrument `PriceStep`. For symbols where the minimal step is not provided, a default 0.0001 pip is assumed.
- No Python port is provided, as requested.
