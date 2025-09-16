# Small Inside Bar Strategy

## Overview
The Small Inside Bar Strategy searches for a compact inside bar pattern followed by a momentum shift between two consecutive candles. The original MetaTrader 5 expert was translated to StockSharp high-level API and now operates on completed candles only. The approach is designed for traders who prefer breakout-style entries triggered by compressed volatility phases.

## Pattern definition
The strategy evaluates the two most recent completed candles:

1. **Inside bar condition** – the latest finished candle must be fully contained within the range of the previous one.
2. **Range ratio filter** – the range of the mother bar (two bars ago) must be at least a configurable multiple of the inside bar range. The default ratio is 2:1.
3. **Directional filters** –
   - A long setup requires a bullish inside bar forming in the lower half of the mother bar together with a bearish mother bar.
   - A short setup requires a bearish inside bar forming in the upper half of the mother bar together with a bullish mother bar.
4. Optional reversal swaps the long and short interpretations while retaining the same geometric requirements.

## Position handling
The `OpenMode` parameter mirrors the behaviour of the original EA:

- **AnySignal** – submit a new market order on every signal. When an opposite position exists it is partially offset because StockSharp uses netting accounts.
- **SwingWithRefill** – flatten the opposite exposure before entering and allow multiple adds in the same direction.
- **SingleSwing** – maintain at most one directional trade; new signals are ignored while an aligned position is open.

Both long and short entries can be independently enabled. Reversal trading simply inverts which setup produces long or short orders.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | 1 hour time frame | Candle subscription used for pattern detection. |
| `RangeRatioThreshold` | 2.0 | Minimum mother-to-inside range ratio. |
| `EnableLong` | true | Allow bullish trades. |
| `EnableShort` | true | Allow bearish trades. |
| `ReverseSignals` | false | Swap long and short pattern directions. |
| `OpenMode` | SwingWithRefill | Controls how existing exposure is handled on a new signal. |

## Trading logic
1. Subscribe to the configured candle series and wait for finished bars.
2. Maintain the last two completed candles to evaluate the pattern.
3. When the pattern and ratio filters align, determine the directional signal, optionally applying reversal.
4. Confirm that trading is allowed (`IsFormedAndOnlineAndAllowTrading`) and that the relevant direction is enabled.
5. Compute the order size based on the selected `OpenMode` and send a market order using the base strategy volume.
6. Update the internal candle history so the newest candle becomes part of the next evaluation cycle.

## Implementation notes
- The strategy uses `StartProtection()` to enable the built-in risk manager (without predefined stop or take-profit values). Extra filters can be added externally if needed.
- Indicator state is not stored in collections; only the two latest candles are kept as required for the pattern.
- The algorithm relies purely on completed candles, avoiding intra-bar calculations in line with high-level API best practices.
