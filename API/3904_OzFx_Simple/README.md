# OzFx Simple Strategy

## Overview
- Conversion of the MetaTrader 4 expert advisor **OzFx** (folder `MQL/7994`) to the StockSharp high-level API.
- Uses the Accelerator/Decelerator oscillator (AC) together with the %K line of the stochastic oscillator to detect momentum reversals around the zero line.
- Replicates the expert's behaviour of stacking five market orders with staggered take-profits and breakeven protection after the first target is hit.

## Trading Logic
1. Build the Awesome Oscillator (5/34) and subtract its 5-period SMA to obtain the Accelerator Oscillator value of the previous and current completed candle.
2. Subscribe to the stochastic oscillator (%K length = `StochasticLength`, smoothing 3/3) and read the main line on candle close.
3. **Long setup** requires:
   - `%K` above the configured mid-level (default 50).
   - Current AC value positive and higher than the previous one.
   - Previous AC value still below zero (momentum crosses the baseline).
4. **Short setup** mirrors the rules in the opposite direction.
5. When a signal appears on a new bar the strategy opens five equal market orders:
   - Layers 1-4 receive take-profits spaced by `TakeProfitPips` multiples.
   - Layer 5 has no profit target and remains to trail the move.
6. If the opposite setup appears while a stack is open the remaining orders are closed at market, keeping the strategy flat before new entries.

## Position Management
- Every layer shares the same stop-loss distance defined by `StopLossPips`.
- After the first take-profit executes, the remaining orders tighten their stops to the breakeven (entry) price, matching the original "modok" logic.
- Protective exits are executed when candle extremes pierce the stored stop or target levels; broker-side pending orders are not used.
- The strategy allows only one direction at a time and waits for all orders to close before resetting the entry block flags.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Lot size for each of the five market orders. | `0.1` |
| `StopLossPips` | Distance between entry and stop loss, expressed in pips. | `100` |
| `TakeProfitPips` | Increment between consecutive take-profit levels (layers 1-4). | `50` |
| `StochasticLevel` | Threshold applied to the stochastic %K value. | `50` |
| `StochasticLength` | Lookback period of the stochastic %K calculation. | `5` |
| `CandleType` | Source candle series used by the strategy (defaults to 4-hour candles). | `4h time-frame` |

## Implementation Notes
- Signals are evaluated only on finished candles to stay consistent with the MT4 expert that works on new bars.
- Pip conversion adapts automatically to 3/5-digit forex symbols by multiplying the minimal price step by 10 when needed.
- Staggered entries and exits are handled in-memory via layered objects so that the strategy can properly close portions of the position.
- All comments inside the C# code are written in English, as required by the repository guidelines.
