# BrainTrend2 + AbsolutelyNoLagLWMA MMRec Strategy

## Overview
This strategy re-creates the MetaTrader expert `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec` by combining two independent signal blocks: the BrainTrend2 trend-following engine and the AbsolutelyNoLagLWMA adaptive filter. Each block can open and close trades according to its own permissions, mimicking the money-management switches of the original MMRec template. Orders are executed with the StockSharp high-level API using market executions and the configurable default volume.

## Trading Logic
### BrainTrend2 Block
* Builds a dynamic trailing level based on an ATR-like weighted true range.
* The direction (`river`) toggles when the candle pierces the trailing buffer by more than `0.7 * ATR`.
* Up candles inside an up river trigger long entries (if enabled) and close short positions.
* Down candles inside a down river trigger short entries (if enabled) and close long positions.
* Signals can be delayed by the `Brain Signal Shift` parameter to work with older bars.

### AbsolutelyNoLagLWMA Block
* Applies a two-stage linear weighted moving average to the selected price source.
* Colours become **up (2)** when the double LWMA rises, **down (0)** when it falls and **neutral (1)** otherwise.
* A transition to colour 2 opens longs and optionally exits shorts; a switch to colour 0 opens shorts and optionally exits longs.
* Signals may also be shifted back by a user-defined number of bars.

### Position Management
* The strategy operates a single net position. When both blocks request trades on the same bar, close signals execute before any new entries.
* If a block wants to open a trade but the opposite position is open and the corresponding close permission is disabled, the entry is skipped (mirrors the inability to hold hedged positions with a single net portfolio).

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | Candle type used for the BrainTrend2 indicator. |
| BrainTrend2 | Brain ATR | ATR period for the internal BrainTrend2 calculations. |
| BrainTrend2 | Brain Signal Shift | Number of bars to delay BrainTrend2 signals. |
| BrainTrend2 | Brain Buy / Sell | Allow BrainTrend2 to open long/short trades. |
| BrainTrend2 | Brain Close Buys / Close Sells | Allow BrainTrend2 signals to close existing positions. |
| AbsolutelyNoLag | Abs Candle | Candle type used for the LWMA indicator. |
| AbsolutelyNoLag | Abs Length | LWMA period. |
| AbsolutelyNoLag | Abs Price | Applied price used for the LWMA. Matches the MQL `Applied_price_` enum. |
| AbsolutelyNoLag | Abs Signal Shift | Number of bars to delay LWMA signals. |
| AbsolutelyNoLag | Abs Buy / Sell | Allow LWMA block to open long/short trades. |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | Allow LWMA block to close positions. |
| AbsolutelyNoLag | Abs Shift | Adds a constant price offset to the LWMA output. |
| General | Order Volume | Default market order volume. |

## Notes
* The ATR and LWMA computations follow the original MQL implementations, including the triangular ATR weighting and the extensive applied price list.
* Spread information is unavailable in StockSharp candles, so the true range uses candle prices only. This mirrors the indicator behaviour when spread equals zero.
* Multiple simultaneous positions with different magic numbers are consolidated into a single net position, which is standard in StockSharp strategies.
