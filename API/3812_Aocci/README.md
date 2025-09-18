# AOCCI Strategy

## Overview
The AOCCI strategy is a direct conversion of the MetaTrader 4 "AOCCI" expert advisor. It combines momentum and mean reversion filters by using the Awesome Oscillator (AO) and the Commodity Channel Index (CCI) together with a daily floor pivot. The converted version works on StockSharp's high level API and keeps the same protective logic as the original script.

## Logic
1. **Data preparation**
   - Uses intraday candles (default 1 hour) for signal generation.
   - Uses daily candles to compute the pivot from the previous completed day (high + low + close divided by 3).
   - Tracks the last six intraday opening prices to detect large gaps.
2. **Gap filter**
   - Any single step difference that exceeds the *Big Jump Filter* threshold cancels the current signal.
   - Any two-step combined difference that exceeds the *Double Jump Filter* threshold also cancels the signal.
3. **Indicator checks**
   - AO must be greater than zero and CCI must be non-negative on the current bar.
   - At least one of the following must be true on the previous bar: AO below zero, CCI at or below zero, or price below the pivot.
4. **Directional filter**
   - The close price must remain above the pivot level.
5. **Orders**
   - The original expert advisor only opens long trades because the short condition duplicates the long logic. The conversion keeps this behaviour.
   - Market orders use the configured *Order Volume*.
6. **Protection**
   - Initial stop-loss and take-profit are expressed in price steps.
   - Optional trailing stop tightens the stop once price moves in favour of the position by at least the trailing distance.

## Parameters
| Name | Description |
| --- | --- |
| `CciPeriod` | Period for the Commodity Channel Index (default 55). |
| `SignalCandleOffset` | Additional offset applied when referencing historical daily candles (default 0). |
| `StopLossPoints` | Stop-loss distance in price steps. |
| `TakeProfitPoints` | Take-profit distance in price steps. |
| `TrailingStopPoints` | Trailing stop distance in price steps (0 disables trailing). |
| `BigJumpPoints` | Maximum allowed single-bar opening gap expressed in price steps. |
| `DoubleJumpPoints` | Maximum allowed two-bar combined gap expressed in price steps. |
| `OrderVolume` | Volume used when submitting market orders. |
| `CandleType` | Intraday candle type (default one-hour bars). |
| `DailyCandleType` | Daily candle type used for pivot calculation. |

## Usage Notes
- The strategy requires both intraday and daily data subscriptions.
- Price step (tick size) from the selected security is used to translate point-based risk parameters into actual prices.
- Trailing stop management is applied on completed candles, mirroring the original EA's behaviour.
- Because the original MQL4 version never triggers short trades, the conversion intentionally keeps the same rule set.
