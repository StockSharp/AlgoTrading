# Bollinger Bands RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A multi-band Bollinger breakout system converted from the "Bollinger Bands RSI" MetaTrader expert advisor. The strategy derives three Bollinger envelopes with identical periods but different deviations to create "yellow", "blue", and "red" bands. Orders are triggered when price revisits configurable zones around these bands, optionally confirmed by RSI and Stochastic filters.

## Strategy Logic
- The primary (yellow) band uses the configured deviation multiplier.
- The blue band halves the deviation, creating a narrower envelope.
- The red band doubles the deviation, producing a wide outer envelope.
- RSI and Stochastic values are evaluated on the previous finished candle (`Bar Shift`) to match the original EA behaviour.
- `Only One Position` controls whether fresh orders are allowed only when the net position is flat or if additional scaling trades are permitted once price returns to the Bollinger middle line.

## Entry Rules
### Long entries
1. Price on the current candle falls to or below the selected long entry zone (`Entry Mode`):
   - Midpoint between yellow & blue, blue & red, or one of the individual bands.
2. Optional confirmations:
   - RSI filter: RSI ≤ `100 - RSI Lower`.
   - Stochastic filter: %K < `100 - Stochastic Lower`.
3. Position prerequisites:
   - If `Only One Position` is enabled, the net position must be flat.
   - Otherwise, additional long orders are blocked until the candle closes above the middle (yellow) band, emulating the EA locking logic.

### Short entries
1. Price on the current candle rallies to or above the selected short entry zone (mirrors the long options).
2. Optional confirmations:
   - RSI filter: RSI ≥ `RSI Lower`.
   - Stochastic filter: %K > `Stochastic Lower`.
3. Position prerequisites mirror the long logic (flat position for single-trade mode or unlocked state once the candle closes back below the middle band).

## Exit Rules
- Closing mode is determined by `Closure Mode`:
  - `Middle Line`: exit longs when price reaches the Bollinger middle band; exit shorts when price touches it from above.
  - `Between Yellow and Blue` / `Between Blue and Red`: exit at the same midpoints used for entries; defaults to midpoints between blue and red when entry mode differs.
  - `Yellow Line`, `Blue Line`, `Red Line`: exit on direct touches of the corresponding upper/lower bands.
- Lock flags for scaling mode are reset automatically when the candle closes on the opposite side of the middle band, recreating the EA behaviour.

## Risk Management
- `Stop Loss` and `Take Profit` parameters are expressed in pips and converted to absolute price distances through `Pip Value` when `StartProtection` is initialised.
- Stops and targets are optional; leave the distance at zero to disable the respective protection leg.
- Trade volume is defined by `Order Volume` and applied to every market order.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Entry Mode` | Chooses the Bollinger zone that triggers entries. | Between yellow & blue |
| `Closure Mode` | Defines the profit-taking band or midpoint. | Between blue & red |
| `Bands Period` | Period length shared by all Bollinger bands. | 140 |
| `Deviation` | Standard deviation multiplier for the yellow band (blue is half, red is double). | 2.0 |
| `Use RSI Filter` | Enables RSI confirmation logic. | false |
| `RSI Period` | RSI averaging period. | 8 |
| `RSI Lower` | Overbought threshold; oversold uses `100 - value`. | 70 |
| `Use Stochastic Filter` | Enables %K confirmation logic. | true |
| `Stochastic Period` | Main %K lookback period (smoothing is fixed at 3/3 SMA). | 20 |
| `Stochastic Lower` | Overbought threshold; oversold uses `100 - value`. | 95 |
| `Bar Shift` | Number of finished bars to look back for indicator values. | 1 |
| `Only One Position` | If enabled, opens new trades only when no position is active. | true |
| `Order Volume` | Volume submitted with each market order. | 1 |
| `Pip Value` | Absolute price value of one pip for stop/target conversion. | 0.0001 |
| `Stop Loss` | Protective stop distance in pips (0 disables). | 200 |
| `Take Profit` | Protective target distance in pips (0 disables). | 200 |
| `Candle Type` | Data type used for calculations (default 1-minute candles). | 1m time-frame |

## Notes
- The strategy processes only completed candles, so `Bar Shift` should remain ≥ 1 to avoid referencing unfinished bars.
- RSI and Stochastic filters use the %K line; the %D line is calculated but not used, mirroring the original EA implementation.
- The conversion keeps comments and signal names in English and follows the StockSharp high-level API guidelines (Bind-based indicator pipeline, no manual buffer access).
