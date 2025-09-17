# BBStrategy Strategy

## Overview

BBStrategy is a Bollinger Bands breakout system converted from the MetaTrader expert advisor "BBStrategy". The strategy tracks two sets of Bollinger Bands with the same period but different deviation multipliers. When price pierces the outer band the algorithm arms a trade, but an actual entry is postponed until price returns into the inner band. This behaviour attempts to avoid buying overextended breakouts or selling deeply oversold conditions while still capturing the continuation move after a volatility expansion.

## Core Logic

1. Subscribe to candles and calculate two Bollinger Bands:
   - **Outer band** uses a configurable deviation multiplier (default 3.0).
   - **Inner band** uses a lower deviation (default 2.0).
2. Detect when the closing price finishes outside the outer band:
   - Above the upper outer band arms a long setup.
   - Below the lower outer band arms a short setup.
3. Enter only if the next completed candle closes back inside the inner band in the direction of the breakout. While the price waits to re-enter, the strategy stays in a "wait" state for the corresponding direction.
4. Submit a single market order when conditions align and there are no open positions or active orders. Existing opposite positions are closed by increasing the volume of the market order.
5. Optional take-profit and stop-loss distances (expressed in points) are converted to absolute price offsets and managed via the built-in protection helper.

## Parameters

| Name | Description |
|------|-------------|
| **Order Volume** | Trade size for each position. |
| **Bollinger Period** | Number of candles used for both Bollinger Band calculations. |
| **Inner Deviation** | Deviation multiplier for the inner band that validates pullbacks. |
| **Outer Deviation** | Deviation multiplier for the outer band that detects breakouts. |
| **Stop-Loss Points** | Protective stop distance in points (0 disables the stop). |
| **Take-Profit Points** | Take-profit distance in points (0 disables the target). |
| **Candle Type** | Candle timeframe for calculations. |

## Notes

- The strategy trades a single position at a time and ignores new signals while orders are active.
- For risk management the helper converts MetaTrader "points" into actual price increments based on the instrument tick size.
- Chart drawings include candles, both Bollinger Bands and the strategy's own trades for easier visual debugging.
