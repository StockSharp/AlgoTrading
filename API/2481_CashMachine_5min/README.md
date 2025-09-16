# Cash Machine 5min Strategy

## Overview
This strategy is a direct conversion of the **CashMachine 5min** expert advisor from MQL to the StockSharp high level API. It is designed for five-minute candles and combines the DeMarker indicator with a Stochastic oscillator crossover filter. Trade management uses hidden stop-loss / take-profit levels together with staged trailing rules that attempt to lock in gains once price momentum appears.

## Trading Logic
### Entry Conditions
- **Long**: Previous DeMarker value below 0.30 and current value at or above 0.30 **and** Stochastic %K crosses above 20 on the same candle. No position must be open.
- **Short**: Previous DeMarker value above 0.70 and current value at or below 0.70 **and** Stochastic %K crosses below 80. No position must be open.

### Position Management
- Only one position is maintained at a time; opposite signals are ignored until the current trade is closed.
- Hidden exits close the position when price touches `Entry ± HiddenStopLoss` or `Entry ± HiddenTakeProfit` (values interpreted in pips).
- Three intermediate profit targets (`TargetTp1/2/3`) move a hidden trailing stop to `current price - (target - 13)` pips for longs and `current price + (target + 13)` pips for shorts. The extra 13 pips mimics the original EA behaviour, locking in profits after each milestone without immediately exiting.
- If the trailing stop is touched after activation, the position is closed at market.

## Indicators
- **DeMarker** – Detects momentum reversals; the length parameter matches the original averaging period.
- **Stochastic Oscillator** – Uses the original %K period (`StochasticLength`), %K smoothing (`StochasticK`) and %D smoothing (`StochasticD`).

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `HiddenTakeProfit` | Hidden take-profit distance in pips. | 60 |
| `HiddenStopLoss` | Hidden stop-loss distance in pips. | 30 |
| `TargetTp1` | First trailing activation level (pips). | 20 |
| `TargetTp2` | Second trailing activation level (pips). | 35 |
| `TargetTp3` | Third trailing activation level (pips). | 50 |
| `DeMarkerLength` | DeMarker averaging period. | 14 |
| `StochasticLength` | Stochastic %K lookback period. | 5 |
| `StochasticK` | %K smoothing length. | 3 |
| `StochasticD` | %D smoothing length. | 3 |
| `CandleType` | Candle series used for calculations (default 5-minute). | 5-minute timeframe |

## Notes
- Pip size is derived from `Security.PriceStep`. When the step is unknown a fallback value of `0.0001` is used, reproducing the EA logic that adjusts for 3- and 5-digit quotes.
- All trading decisions are based on finished candles; intra-bar behaviour of the original EA may differ slightly because the MQL version ran on every tick.
- The strategy relies on StockSharp's standard order volume handling—set `Strategy.Volume` to control trade size.
