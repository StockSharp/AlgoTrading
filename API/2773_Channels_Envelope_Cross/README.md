# Channels Envelope Cross Strategy

## Overview

The Channels Envelope Cross strategy is a direct port of the MetaTrader "Channels" expert advisor. The system trades hourly candles and monitors a fast two-period exponential moving average (EMA) relative to three EMA-based envelopes (0.3%, 0.7% and 1.0% deviations) that are calculated from a slow 220-period EMA. Breakouts of the fast EMA through these envelopes generate directional entries, while an optional time filter restricts trading to specific hours.

## Trading Logic

1. **Indicator stack**
   - Fast EMA (length 2) calculated on candle close prices.
   - Fast EMA (length 2) calculated on candle open prices.
   - Slow EMA (length 220) calculated on candle close prices.
   - Three envelope levels derived from the slow EMA with 0.3%, 0.7% and 1.0% deviations.
2. **Long setup**
   - Triggered when the fast close EMA crosses above either the 1.0% or 0.7% lower envelope, remains below the 0.3% lower envelope for two consecutive bars, crosses above the slow EMA, or breaks through the 0.3% or 0.7% upper envelope. Any of these conditions can fire a long entry when no position is open.
3. **Short setup**
   - Triggered when the fast open EMA crosses below any of the upper envelopes, drops below the slow EMA, or pierces the lower envelopes from above. Any of these conditions can fire a short entry when no position is open.
4. **Risk management**
   - Fixed stop-loss and take-profit levels (per side) are expressed in pips and converted to price distance by using the instrument tick size. If the inputs are set to zero, the respective level is not applied.
   - Independent trailing stops for long and short positions move the protective stop closer to market price when the profit exceeds the trailing distance plus a configurable step increment.
5. **Time filter**
   - When enabled, the strategy only processes entries during the configured inclusive hour range. Positions are still managed when the filter is active.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Order size used for market entries (lots or contracts depending on the security). |
| `UseTradeHours` | Enables the time filter for entries. |
| `FromHour` / `ToHour` | Inclusive start and end hours for the trading window (supports overnight ranges). |
| `StopLossBuyPips` / `StopLossSellPips` | Stop-loss distance for long/short trades expressed in pips. |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Take-profit distance for long/short trades expressed in pips. |
| `TrailingStopBuyPips` / `TrailingStopSellPips` | Trailing stop distance in pips for long/short trades. |
| `TrailingStepPips` | Minimum increment (in pips) required to move a trailing stop. |
| `CandleType` | Candle series used for calculations (default is 1-hour time frame). |

## Position Management

- On entry the strategy stores the fill price, calculates stop-loss and take-profit targets in absolute price units, and resets trailing levels.
- While a long position is open, the stop-loss is trailed upward whenever profit exceeds `TrailingStopBuyPips + TrailingStepPips`. The strategy exits at the stop-loss or take-profit whichever is hit first.
- While a short position is open, the stop-loss is trailed downward using the short-side trailing parameters and exits are executed symmetrically.

## Notes

- The pip size is derived from the security tick size. For three- or five-decimal instruments the pip is multiplied by ten to emulate the MetaTrader logic.
- The strategy works with a single position at a time. A new entry is only placed after the existing position has been closed.
- Enable `StartProtection` in the base class to guard against unexpected open positions after restarts (already called in the implementation).
