# FT Bill Williams AO Strategy

## Overview
The **FT Bill Williams AO Strategy** is a high-level StockSharp port of the MetaTrader 4 expert `FT_BillWillams_AO`. The original
robot was published on FORTRADER.RU and combines Bill Williams fractals, the Alligator indicator, and the Awesome Oscillator to
identify early breakout opportunities. The StockSharp version keeps the original logic but works with a single net position instead of
multiple simultaneous orders.

The algorithm operates on completed candles from a configurable timeframe. Every bar it:

1. Detects bullish and bearish fractals built from an odd number of candles.
2. Filters fractals by checking whether the fractal price is outside the Alligator teeth line.
3. Waits for the Awesome Oscillator (AO) to form the classic three-bar acceleration pattern.
4. Places a breakout trigger above/below the recent high or low shifted by a user-defined number of MetaTrader points.
5. Applies Bill Williams' Gragus trailing routine and optional jaw-based exit rules.

## Entry logic
### Long entries
- A bullish fractal appears and its high price sits above the Alligator teeth.
- AO values taken `SignalShift + 2`, `SignalShift + 1`, and `SignalShift` candles ago satisfy `A > B`, `B < C`, and all three are
  positive.
- A pending breakout level is calculated as `High[SignalShift] + IndentPoints * price step`.
- When a completed candle crosses that level and AO still increases (`C > B`), the strategy opens or reverses into a long position.

### Short entries
- A bearish fractal appears and its low is below the Alligator teeth.
- AO values satisfy `A < B`, `B > C`, and all three are negative.
- A breakout trigger is placed at `Low[SignalShift] - IndentPoints * price step`.
- A short position (or reversal from long) is opened when the candle dips below that trigger while AO keeps falling (`C < B`).

## Exit and risk management
- Initial stop-loss and take-profit are expressed in MetaTrader points and translate into actual price distance via the instrument
  price step.
- The **CloseDropTeeth** mode can close positions when either the current close or the previous close crosses the Alligator jaw.
- **CloseReverseSignal** determines whether an opposite fractal or the activation of the opposite breakout signal should force an
  exit.
- The **UseTrailing** switch enables the original Gragus trailing stop routine: when the Alligator lips advance faster than a short
  SMA, the stop is moved to the lips; otherwise it trails the teeth. Both moves require the price to stand at least 12 points away
  from the target line.

## Parameters
| Name | Description |
| --- | --- |
| `TradeVolume` | Order size in lots. It is also written to `Strategy.Volume`. |
| `CandleType` | Data type and timeframe of the input candles. |
| `FractalPeriod` | Odd number of candles used to confirm fractals (default 5). |
| `IndentPoints` | MetaTrader points added above/below the breakout candle high/low. |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Length of the smoothed moving averages used by the Alligator lines. |
| `JawShift`, `TeethShift`, `LipsShift` | Forward displacement (in candles) applied to the Alligator lines. |
| `CloseDropTeeth` | Behaviour of the jaw-based close rule: disabled, current close crossing, or previous close crossing. |
| `CloseReverseSignal` | Exit condition on opposite signals: disabled, on new fractal, or once the opposite breakout is armed. |
| `UseTrailing` | Enables or disables the Gragus trailing stop routine. |
| `TrendSmaPeriod` | Period of the auxiliary SMA used by the trailing comparison. |
| `StopLossPoints` | Initial stop-loss distance in MetaTrader points. Set to zero to disable. |
| `TakeProfitPoints` | Initial take-profit distance in MetaTrader points. Set to zero to disable. |
| `SignalShift` | Number of fully closed candles skipped when reading AO values and recent highs/lows. |

## Notes
- The strategy assumes the security exposes a valid `PriceStep` (falls back to `MinPriceStep`); if both are missing, a default of
  `0.0001` is used.
- Only one net position is managed. Reversal signals automatically close the opposite position before opening a new one.
- For best results keep `FractalPeriod` odd; the original expert used 5 candles.
- `IndentPoints`, `StopLossPoints`, and `TakeProfitPoints` mimic MetaTrader points. Adjust them according to the instrument's price
  scale.
