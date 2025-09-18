# Artificial Intelligence Right Strategy

## Overview
The strategy replicates the MetaTrader 4 expert advisor **ArtificialIntelligence_Right.mq4**. It evaluates a single-layer
perceptron built on the Acceleration/Deceleration (AC) oscillator to decide when the market momentum changes direction. The
perceptron uses four delayed AC samples and turns those into a signed signal that drives both entries and reversals.

Unlike the original EA, the StockSharp port works on the high-level candle API. Price actions are taken on the close of each
finished candle, which keeps the logic deterministic for backtests and optimization workflows.

## Indicators and Calculations
- **Acceleration/Deceleration oscillator** is rebuilt from the Awesome Oscillator by subtracting a 5-period SMA from the AO
  values (5-period SMA of `HL2` minus 34-period SMA of `HL2`).
- A circular buffer stores 22 most recent AC values so the perceptron can access the offsets 0, 7, 14, and 21, exactly matching
  the MetaTrader implementation.
- The perceptron weights are shifted by `-100` before the dot product, reproducing the `w = x - 100` logic of the source code.

## Trading Rules
1. **Entry conditions**
   - When the perceptron output is positive and the strategy is flat, a market buy order is submitted.
   - When the perceptron output is negative and the strategy is flat, a market sell order is submitted.
2. **Stop-loss management**
   - A virtual protective stop is assigned after every entry at a distance equal to `StopLossPoints * PriceStep` away from the
     entry price. This emulates the `Point` multiplier from MetaTrader.
   - If the closing price crosses this level, the position is exited at market to mimic the stop-loss order execution.
3. **Trailing and reversal**
   - Once the position floats in profit by `(2 * StopLossPoints + SpreadPoints)` points, the original robot either starts
     trailing the stop by the stop-loss distance or reverses if the perceptron changes its sign.
   - The StockSharp version uses the same trigger: when the profit threshold is reached, if the perceptron flipped direction,
     a market order with double the current exposure is issued to reverse the trade; otherwise, the virtual stop is trailed to
     preserve the original distance from the current close.

All reversals are performed by trading double the open volume so the resulting position mirrors the MetaTrader `OrderCloseBy`
behaviour, ending up with the opposite direction but the same lot size.

## Parameters
| Name | Description |
| --- | --- |
| `X1` … `X4` | Perceptron weights. Defaults replicate the `.mq4` source (135, 127, 16, 93). |
| `StopLoss` | Stop-loss distance expressed in MetaTrader points. It is multiplied by the instrument `PriceStep` to obtain a real price offset. |
| `Spread` | Additional spread buffer (default 3 points) used in the trailing trigger condition. |
| `Candle Type` | Candle series used for calculations. Defaults to 1-minute time frame. |

The `Volume` property is pre-set to 1 lot, mirroring the `lots` input parameter of the original expert.

## Implementation Notes
- Indicator calculations and the perceptron state are reset whenever the strategy is reset to prevent stale values from causing
  false triggers.
- If the security does not provide a `PriceStep`, the strategy falls back to a point value of `1`, maintaining compatibility
  with generic backtesting instruments.
- No real stop orders are registered; instead, the stop logic is executed via market orders in the candle handler. This keeps the
  behaviour consistent across brokers and simulators.
