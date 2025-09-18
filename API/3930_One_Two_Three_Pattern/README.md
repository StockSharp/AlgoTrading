# One-Two-Three Pattern Strategy

## Overview

This strategy reproduces the MetaTrader 4 expert advisor “1-2-3_forCodeBase_v01.mq4” by Martes. It scans finished candles for the classical 1-2-3 reversal pattern: two consecutive trend legs completed by a third retracement leg. The port keeps every rule of the original system, including the custom trend-length indicators (`RelDownTrLen_forCodeBase_v01` and `RelUpTrLen_forCodeBase_v01`) and the MACD confirmation logic.

A long setup requires a fresh valley (point 3) near the current price, a preceding peak (point 2) and an older valley (point 1). The previous down-trend must be at least `TrendRatio` times longer than the current up retracement, and MACD has to cross above the signal line (or zero) while remaining positive at point 3. The short side mirrors those checks with peaks and valleys inverted. Stops are placed one point beyond point 3, the take-profit equals the height of the previous swing, and an optional pip-based trailing stop tightens the exit once the trade moves into profit.

## Trading rules

1. Subscribe to the configured candle series (`CandleType`) and compute MACD (fast/slow/signal periods) on the closing prices.
2. Maintain a rolling history of candle bodies to detect the 1-2-3 structure. Valleys are local minima of the candle bodies, peaks are local maxima.
3. Evaluate the custom trend length metrics using the convex-hull method from the MQL indicators. The latest down-trend length (scaled to `[0,1]`) must dominate the preceding up-trend (and vice versa for shorts) according to `TrendRatio`.
4. Confirm the setup with MACD:
   - Long: `MACD` crosses above the signal (or above zero) and the MACD value at point 3 is positive.
   - Short: `MACD` crosses below the signal (or below zero) and the MACD value at point 3 is negative.
5. Additional entry filters:
   - Distance from the current price to point 2 must be within five points.
   - The projected stop distance (`|point2 - point3|`) must be at least 13 points.
   - `TakeProfitPips` must remain ≥ 10; otherwise, trading is disabled (mirrors the original safety check).
6. Order handling:
   - Enter using `BuyMarket`/`SellMarket` with `TradeVolume` lots (aggregated with the current position volume for reversals).
   - Initial stop loss = point 3 ± one price step.
   - Take profit = entry ± `|point2 - point3|`.
   - If `TrailingStopPips` > 0, trail the stop by that many points once unrealised profit exceeds the trailing distance.
7. Exit on stop, take profit, or trailing stop. Only one position can be open at a time.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `TakeProfitPips` | `decimal` | `60` | Compatibility flag from the EA. Trading stops if the value is set below 10. |
| `TradeVolume` | `decimal` | `0.5` | Volume in MetaTrader lots used for each market order. |
| `TrailingStopPips` | `decimal` | `30` | Trailing stop distance in MetaTrader points. Set to `0` to disable trailing. |
| `TrendRatio` | `decimal` | `4` | Minimum ratio between the previous main trend length and the recent retracement. |
| `CandleType` | `DataType` | `H1` | Candle series used for pattern and MACD calculations. |
| `MacdFast` | `int` | `12` | Fast EMA period of the MACD oscillator. |
| `MacdSlow` | `int` | `26` | Slow EMA period of the MACD oscillator. |
| `MacdSignal` | `int` | `9` | Signal line EMA period. |
| `PatternLookback` | `int` | `100` | Maximum number of historical candles scanned when locating the 1-2-3 points. |

## Implementation notes

- The original custom indicators are ported verbatim: convex-hull searches compute the longest monotonic segments of candle bodies and return their relative lengths in `[0,1]`. These values drive the trend ratio filter.
- Historical candles and MACD values are stored in bounded buffers (600 elements) to avoid excessive memory usage while keeping enough depth for the lookback.
- Stops and targets are managed manually to match MetaTrader behaviour: prices are compared against candle highs/lows, and the trailing stop only tightens when price progresses by at least the configured distance.
- `Volume` is synchronised with `TradeVolume` on reset and on start, so optimisation can rely on the standard strategy property.

## References

- Original MQL4 Expert Advisor: `MQL/8131/1-2-3_forCodeBase_v01.mq4`.
- Custom indicators: `RelDownTrLen_forCodeBase_v01.mq4`, `RelUpTrLen_forCodeBase_v01.mq4`.
