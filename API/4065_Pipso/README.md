# Pipso Strategy

## Overview
Pipso is a night-session breakout system converted from the MetaTrader expert advisor `Pipso.mq4`. The strategy measures the
highest and lowest prices of the previously completed candles and reacts when the market breaks outside of that range. Every
breakout reverses the position: long positions are closed and a short is opened when price breaks above the recent highs, while
short positions are covered and a new long is established when price pierces the recent lows. Protective stops are derived from
the width of the range so that the stop distance automatically adapts to current volatility.

## How It Works
1. Subscribe to the configured time-frame (15 minutes by default) and wait for the indicators to build a complete history.
2. For every new finished candle, compute the highest high and lowest low of the previous `BreakoutPeriod` candles. The current
   candle is not part of that range, exactly as in the original EA where `iHighest(..., shift = 1)` skips the working bar.
3. Recalculate the stop distance as `(high - low) * StopLossMultiplier` while enforcing the minimum distance defined by
   `MinStopDistance`.
4. Maintain a trading window defined by `SessionStartHour` and `SessionLengthHours`. When the window crosses midnight on Friday
   it is extended by two days so that open trades survive the weekend just like in MetaTrader.
5. When the candle's high exceeds the stored breakout high:
   - Close any existing long position and, if trading is allowed, open a short position with size `OrderVolume`.
   - Attach a stop-loss above the entry price using the calculated stop distance.
6. When the candle's low falls below the stored breakout low:
   - Close any existing short position and, if trading is allowed, open a long position with size `OrderVolume`.
   - Attach a stop-loss below the entry price using the calculated stop distance.
7. Protective stops are evaluated on every finished candle. If the low touches the long stop or the high reaches the short stop,
   the position is flattened immediately.

## Trading Session Logic
- `SessionStartHour` is expressed in exchange hours. The window length is set with `SessionLengthHours`.
- If the session extends beyond 24 hours and the current day is Friday, the end of the window is shifted forward by 48 hours so
  that trading resumes on Monday, matching the weekend handling in the MQL4 code.
- Outside of the trading window the strategy only closes existing positions; new trades are allowed again once the window opens.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle data type used for signal calculation. | 15-minute time-frame |
| `OrderVolume` | Fixed order size for every market order. | 1 |
| `SessionStartHour` | Hour of day when the breakout window begins. | 21 |
| `SessionLengthHours` | Duration of the trading window in hours. | 9 |
| `BreakoutPeriod` | Number of completed candles that define the breakout range. | 36 |
| `StopLossMultiplier` | Multiplier applied to the range width to derive the stop distance (value `3` corresponds to the original `SLpp = 300`). | 3 |
| `MinStopDistance` | Minimal stop-loss distance in absolute price units, emulating the MetaTrader stop level restriction. | 0 |

## Notes
- The strategy uses market orders only; there is no take-profit. The protective stop-loss is the only exit mechanism besides
  the opposite breakout signal.
- When switching from long to short (or vice versa) the strategy sends a single market order that both closes the previous
  position and opens the new one, mirroring the behaviour of the source EA that sequentially called `OrderClose` and
  `OrderSend`.
- Indicator lines for the breakout highs and lows are plotted automatically on the strategy chart together with executed trades.
