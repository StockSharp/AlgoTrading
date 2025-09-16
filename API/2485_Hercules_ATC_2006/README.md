# Hercules A.T.C. 2006 Strategy

## Overview

Hercules A.T.C. 2006 is a high time frame trend-following strategy that recreates the
MetaTrader expert advisor published in 2006. The StockSharp version listens to completed
candles on the primary time frame, watches for a bullish/bearish crossover between a fast
EMA(1) and a slow SMA(72), and opens trades only when additional filters confirm the
breakout. The strategy splits its position into two tranches with independent take-profit
levels and trails the stop once price advances.

## Indicators and Data

- **Primary candles:** configurable (defaults to 1-hour candles).
- **Fast MA:** EMA with length `FastMaPeriod` (default 1).
- **Slow MA:** SMA with length `SlowMaPeriod` (default 72).
- **RSI filter:** RSI of length `RsiLength` on the `RsiTimeFrame` (default 1-hour).
- **Daily envelope:** SMA of length `DailyEnvelopePeriod` on `DailyEnvelopeTimeFrame`
  with ±`DailyEnvelopeDeviation` percent offset.
- **H4 envelope:** SMA of length `H4EnvelopePeriod` on `H4EnvelopeTimeFrame`
  with ±`H4EnvelopeDeviation` percent offset.
- **Rolling high/low:** highest high and lowest low for the past `HighLowHours`
  hours on the primary time frame.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `TriggerPips` | 38 | Offset in pips added/subtracted to the crossover price before triggering an order. |
| `TrailingStopPips` | 90 | Trailing stop distance in pips (0 disables trailing). |
| `TakeProfit1Pips` | 210 | First take-profit distance in pips for scaling out half of the position. |
| `TakeProfit2Pips` | 280 | Final take-profit distance in pips used to close the remaining position. |
| `FastMaPeriod` | 1 | Length of the fast EMA used in the crossover detector. |
| `SlowMaPeriod` | 72 | Length of the slow SMA baseline. |
| `StopLossLookback` | 4 | Number of completed candles used to pull the initial stop price. |
| `HighLowHours` | 10 | Size of the rolling window (in hours) used for the breakout filter. |
| `BlackoutHours` | 144 | Cooldown period (in hours) after a trade closes before a new entry is allowed. |
| `RsiLength` | 10 | RSI length on the higher time frame filter. |
| `RsiUpper` | 55 | Minimum RSI value required to allow long entries. |
| `RsiLower` | 45 | Maximum RSI value allowed before short entries are blocked. |
| `DailyEnvelopePeriod` | 24 | SMA length for the daily envelope filter. |
| `DailyEnvelopeDeviation` | 0.99 | Daily envelope deviation in percent. |
| `H4EnvelopePeriod` | 96 | SMA length for the four-hour envelope filter. |
| `H4EnvelopeDeviation` | 0.1 | Four-hour envelope deviation in percent. |
| `CandleType` | 1 hour | Primary working candle type. |
| `RsiTimeFrame` | 1 hour | Candle type used for the RSI filter. |
| `DailyEnvelopeTimeFrame` | 1 day | Candle type used for the daily envelope. |
| `H4EnvelopeTimeFrame` | 4 hours | Candle type used for the four-hour envelope. |

## Trading Rules

1. **Crossover detection**
   - Watch the EMA(1) and SMA(72) values from the last three completed bars.
   - Detect a bullish signal when EMA crosses above SMA during either of the two previous bars.
   - Detect a bearish signal when EMA crosses below SMA during either of the two previous bars.
   - Store the crossover price (average of the fast and slow values) and start a two-bar trigger window.

2. **Trigger condition**
   - Calculate `TriggerPrice = CrossPrice ± TriggerPips` (converted to price units).
   - The trigger remains valid for two primary candles after the crossover time.
   - Longs require the candle high to reach or exceed the bullish trigger price.
   - Shorts require the candle low to reach or break the bearish trigger price.

3. **Entry filters**
   - No existing position and no open cooldown (`BlackoutHours`).
   - RSI filter: `RSI > RsiUpper` for longs, `RSI < RsiLower` for shorts.
   - Breakout filter: current close must exceed the rolling high for longs or fall below the rolling low for shorts.
   - Envelope confirmation: current close must be above both upper envelope bands for longs or below both lower bands for shorts.

4. **Order execution**
   - Submit a market order using the strategy volume (defaults to 2 units, meaning two equal sub-positions).
   - Stop loss: previous `StopLossLookback`-th candle low (long) or high (short).
   - Take-profit levels: `TakeProfit1Pips` for the first half, `TakeProfit2Pips` for the remainder.
   - Start a blackout timer to block new entries for `BlackoutHours` hours.

5. **Position management**
   - Trailing stop activates immediately if `TrailingStopPips` > 0 and moves in favor of the trade only.
   - Scale-out half of the position at the first take-profit level.
   - Close the remaining position when the final take-profit triggers, the stop loss is hit, or price crosses the trailing stop.

## Risk Management

- Stops are always derived from completed candles to reduce intrabar noise.
- Two take-profit targets lock in partial profits before letting the trade run.
- Trailing stops ensure gains are protected after the market moves in the desired direction.
- A long blackout period (default 144 hours) prevents rapid re-entry after a breakout and mirrors the original EA behaviour.

## Notes

- The StockSharp port preserves the original money-management idea by defaulting the strategy volume to two units, so the partial exit leaves half of the position running.
- Envelope shift values from MetaTrader are approximated by using the most recent envelope values because forward shifting is not supported by the high-level API.
- The strategy requires price step information to translate pip distances correctly; ensure the security metadata is populated.
