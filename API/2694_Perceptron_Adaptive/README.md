# Perceptron Adaptive Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader 5 expert advisor *Perceptron.mq5*.  
Five discrete indicator signals are combined through a two-layer perceptron. Each trade records the indicator state and, once the position is closed, synaptic weights are reinforced or penalized depending on the achieved profit. The behaviour mimics the self-learning loop of the original EA while leveraging the StockSharp high-level candle API.

## Indicator layer

| Code | Description | Signal logic |
| --- | --- | --- |
| `IND1` | Fast/slow simple moving average crossover | +1 when the fast MA crosses above the slow MA on the previous bar, −1 on a downward cross, otherwise 0. |
| `IND2` | Relative Strength Index (RSI) | +1 when RSI leaves the oversold area (crosses above 30), −1 when RSI leaves the overbought area (crosses below 70). |
| `IND3` | Commodity Channel Index (CCI) | +1 on a cross above −100, −1 on a cross below +100. |
| `IND4` | Short simple moving average slope | +1 if the short MA increased between the two previous bars, −1 if it decreased. |
| `IND5` | Awesome Oscillator momentum colour | +1 when the histogram increases compared with the previous value (bullish colour), −1 when it decreases. |

All indicators are evaluated on closed candles. Historical buffers are maintained internally to replicate the `CopyBuffer` windowing used in the MQL5 script.

## Perceptron architecture

- Five hidden neurons (`NN1`…`NN5`) combine four indicators each, mirroring the wiring in the EA.
- Each neuron has its own dictionary of synaptic weights plus a bias weight (`NNS1`…`NNS5`).
- The final activation `brainReturn` is the weighted sum of neuron outputs.  
  - `brainReturn > 0` → request a long entry (if the previous trade was not also long).  
  - `brainReturn < 0` → request a short entry (if the previous trade was not also short).
- Positions are opened with market orders only when no position is active.

## Position management

- Entry price, direction and indicator/neuron states are captured on every fill.
- Take-profit and stop-loss offsets are applied in absolute price units (e.g. 0.0004 for 4 points on a Forex pair with 5 decimals).  
  When a new candle opens after the entry:
  - For longs the high is compared with the take-profit price first, then the low with the stop-loss.  
  - For shorts the low is compared with the take-profit price first, then the high with the stop-loss.  
  - If both levels are exceeded inside the same candle the take-profit has priority, matching the optimistic behaviour of the original EA.
- Once an exit is detected the strategy closes the position with a market order and computes the realised profit using the corresponding TP/SL level.

## Adaptive weight update

When a trade closes, the captured indicator and neuron signs are replayed:

1. Determine `directionSign` (−1 for longs, +1 for shorts) and `outcomeSign` (sign of realised PnL).
2. Bias weights are adjusted inside `[SinMin, SinMax]`:
   - If `sign(neuronOutput) * directionSign` is positive the bias follows the trade outcome (increase on profit, decrease on loss).
   - Otherwise the bias moves opposite to the outcome.
3. Synaptic weights behave similarly but remain unbounded: signals aligned with the position direction receive reinforcement on profits and penalties on losses, while opposing signals do the inverse.
4. Stored signals are cleared to avoid accidental re-use.

This generalises the 1,500+ lines of conditional synapse management from the EA into a compact reinforcement routine.

## Parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `CandleType` | 1-minute time frame | Candle subscription used by the strategy. |
| `FastMaLength` | 5 | Period of the fast SMA in the crossover signal. |
| `SlowMaLength` | 9 | Period of the slow SMA. |
| `RsiLength` | 14 | RSI calculation period. |
| `CciLength` | 14 | CCI calculation period. |
| `SlopeMaLength` | 5 | Period of the MA used for slope detection. |
| `AoShortLength` | 5 | Short period of the Awesome Oscillator. |
| `AoLongLength` | 34 | Long period of the Awesome Oscillator. |
| `StopLossOffset` | 0.001 | Stop-loss distance in absolute price units (0 disables the stop). |
| `TakeProfitOffset` | 0.0004 | Take-profit distance in absolute price units (0 disables the target). |
| `SinMax` | 5 | Upper bound for neuron bias weights. |
| `SinMin` | 0 | Lower bound for neuron bias weights. |
| `SinPlusStep` | 0.03 | Positive reinforcement increment. |
| `SinMinusStep` | 0.03 | Negative reinforcement decrement. |

All numeric parameters are exposed as `StrategyParam<T>` and can be optimised inside StockSharp Designer.

## Implementation notes

- Uses the high-level candle subscription API with multi-indicator binding.
- Manual trade management is employed so that realised prices are known when updating synapses.
- Indicator histories are stored with nullable fields to ensure signals only fire after full formation.
- The Awesome Oscillator colour buffer in the EA is approximated by comparing current and previous histogram values.
- Chart output draws the candle series plus the fast and slow moving averages. Trade markers show adaptive behaviour in real time.

## Limitations and assumptions

- Stops and targets are evaluated once per finished candle; intra-bar order of events is unknown, so priority is given to the profit target when both thresholds are hit.
- Indicator weights are unbounded like in the original EA and may grow large during prolonged reinforcement cycles.
- The original EA’s `LastTradeType` never reset; in this port it is cleared after every exit so that consecutive trades in the same direction remain possible.

