# Farhad Crab Strategy (C#)

## Overview
The Farhad Crab strategy is a trend-following system that enters trades on pullbacks to an exponential moving average (EMA) and manages exits using fixed stops, take-profits, a Parabolic SAR-inspired trailing stop, and a higher time frame filter. The original MetaTrader 5 expert advisor relies on hourly candles for execution while referencing daily data to decide when to close open positions. This C# port keeps the same core logic by combining a working timeframe EMA filter with a daily EMA crossover exit rule.

## Core Concepts
- **Trend filter:** An EMA calculated on the working timeframe (default 15-period EMA on 1-hour candles). Only long signals are allowed when the previous candle's low remains above the EMA, and only short signals are allowed when the previous candle's high stays below the EMA.
- **Daily filter:** A separate EMA computed on daily candles. When the daily EMA crosses above the daily close, all long positions are closed. When it crosses below, all short positions are closed. This mimics the original `ClosePositions` logic from the MQL5 code.
- **Risk controls:** Fixed stop-loss and take-profit levels are derived from pip distances. A trailing stop moves the protective stop once the position gains enough profit, emulating the MT5 trailing function that combines `TrailingStop` and `TrailingStep` settings.
- **One-position management:** The strategy trades a single net position. Entering a long position while holding a short (or vice versa) first closes the opposite exposure before opening the new trade, aligning with how MT5 hedging positions were aggregated in the conversion.

## Trading Rules
1. **Signal detection (working timeframe):**
   - Long entry when the previous candle's low is greater than the EMA value (after applying the configured shift).
   - Short entry when the previous candle's high is less than the EMA value.
2. **Position sizing:** The `Volume` parameter sets the base order size. When reversing from a short to a long (or vice versa), the engine automatically sends the additional quantity required to flip the net position.
3. **Stop-loss & take-profit:**
   - Distances are defined in pips. Pip size automatically adapts to the security's tick size, with five-digit and three-digit FX symbols using a 10x multiplier to match MT5 behaviour.
   - Stop-loss or take-profit can be disabled by setting the respective pip distance to zero.
4. **Trailing stop:**
   - Activates only when `TrailingStopPips` is greater than zero.
   - The stop is moved to `current_price - TrailingStopPips` (for longs) or `current_price + TrailingStopPips` (for shorts) once the position profit exceeds `TrailingStopPips + TrailingStepPips`.
   - The additional trailing step prevents frequent modifications, matching the EA's use of `TrailingStep`.
5. **Daily exit filter:**
   - Uses the last two completed daily candles.
   - Long positions are closed if the daily EMA was below the daily close two days ago and is above the daily close on the most recent day (bearish crossover).
   - Short positions are closed if the opposite crossover occurs.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-hour time frame | Working time frame used for the execution EMA and entry logic. |
| `MaLength` | `int` | 15 | Period of the EMA on the working time frame. |
| `MaShift` | `int` | 0 | Number of completed candles used to shift the EMA backwards. |
| `DailyMaLength` | `int` | 15 | Period of the daily EMA that provides the crossover exit filter. |
| `StopLossPips` | `decimal` | 50 | Stop-loss distance in pips. Set to `0` to disable. |
| `TakeProfitPips` | `decimal` | 50 | Take-profit distance in pips. Set to `0` to disable. |
| `TrailingStopPips` | `decimal` | 10 | Trailing stop distance in pips. Set to `0` to disable trailing. |
| `TrailingStepPips` | `decimal` | 5 | Minimum additional gain in pips before the trailing stop is updated again. |
| `Volume` | `decimal` | 0.1 | Base trade size in lots/contracts. |

## Notes & Differences from the MQL Version
- This port always uses exponential moving averages, reflecting the original default (`MODE_EMA`). Other MT5 smoothing modes are not supported.
- The MT5 expert advisor works with bid/ask quotes on every tick. This translation operates on finished candles, so stop-loss and take-profit checks are evaluated on candle highs/lows.
- The Parabolic SAR indicator present in the original file did not influence trade decisions and is therefore omitted from the C# implementation.
- Trailing logic adjusts the stored stop level but does not send broker stop orders. The exit occurs when the candle range touches the calculated stop or take-profit level.

## Usage Tips
- Choose a candle type that matches the desired trading horizon. The default one-hour candles replicate the behaviour of the source script.
- Adjust `MaLength` and `DailyMaLength` together to tune responsiveness between intra-day entries and higher time frame trend filters.
- For FX symbols quoted with five digits (e.g., EURUSD), pip distances will be automatically scaled so that 1 pip equals 0.0001.
- When running in backtests, ensure the daily data stream is available so that the exit filter can function correctly.
