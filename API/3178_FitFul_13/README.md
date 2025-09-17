# FitFul 13 Strategy

## Overview
The FitFul 13 expert advisor works around weekly pivot levels derived from the previous trading week. It waits for the current H1 candle (default timeframe) to react to one of the pivot bands and confirms the move with two older candles from an M15 confirmation series. When the confirmation is present the strategy opens a position with pre-calculated stop-loss and take-profit levels derived from the same pivot structure. A trailing stop protects profitable trades once price travels far enough.

## Original logic
1. Compute the previous week's typical price and pivot structure: `PriceTypical`, `R1`, `S1`, intermediate half-levels (`R0.5`, `S0.5`, `R1.5`, etc.) and the second/third extensions.
2. Watch the most recent H1 candle. If it closed bullish, search the body of the preceding candle for an upward cross of one of the pivot levels. If such a cross occurs, prepare long parameters: stop below the relevant support, take-profit above the paired resistance. For bearish closes the mirrored logic prepares short parameters.
3. If the H1 candle body did not interact with any pivot, check two earlier M15 candles. Two consecutive lows piercing the same level confirm long setups, while two highs falling through a level confirm shorts. Each combination maps to its own stop/take pair.
4. Submit a market order with the configured net volume. The StockSharp port works with net positions, therefore opposite exposure is flattened before opening the new trade. Stop-loss and take-profit prices are stored internally and enforced via virtual exits on new candles.
5. Apply a virtual trailing stop: once the open profit exceeds `TrailingStopPips + TrailingStepPips`, move the stop to `close - TrailingStopPips` (long) or `close + TrailingStopPips` (short). The stop never moves backwards and is tightened only if price advances by at least the trailing step.
6. Ignore new signals if the absolute net position already equals `Volume × MaxPositions`.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `CandleType` | `DataType` | H1 | Main timeframe used to evaluate pivot reactions. |
| `ConfirmationCandleType` | `DataType` | M15 | Lower timeframe that provides the two-bar confirmation. |
| `Volume` | `decimal` | 0.1 | Net order volume for each entry. |
| `MaxPositions` | `int` | 3 | Maximum net exposure expressed as multiples of `Volume`. |
| `IndentPips` | `decimal` | 3 | Offset applied to pivot-based stop-loss and take-profit calculations. |
| `TrailingStopPips` | `decimal` | 150 | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | `decimal` | 5 | Minimum additional price progress (in pips) required before tightening the trailing stop. |

## Notes on the port
- StockSharp manages a single net position. The original hedging capability is emulated by flattening opposite exposure when a new entry is taken.
- Stop-loss, take-profit and trailing logic are implemented virtually. The strategy closes positions on candle updates when price crosses the stored levels.
- Weekly pivots are recalculated every time a new weekly candle is received. The default confirmation uses H1/M15, but both timeframes can be adjusted through parameters.
- All comments in the source code are written in English as required by the conversion guidelines.
