# SilverTrend ColorJFatl Digit MMRec Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor `Exp_SilverTrend_ColorJFatl_Digit_MMRec`. It recreates the dual-block architecture where two independent logic modules manage their own virtual position sizes and combine them into the final strategy position:

- **SilverTrend block** – reads candle colors produced by the SilverTrend indicator to detect when price crosses adaptive channel borders.
- **ColorJFatl block** – calculates a filtered FATL (Fast Adaptive Trend Line) using the published weight table and an EMA-based smoother that emulates the Jurik moving average used in MetaTrader.

Both modules can open long and short trades independently, close opposite exposure on new signals, and apply their own stop-loss and take-profit distances. The final position of the strategy equals the sum of the virtual positions handled by the two blocks.

## Default setup

- Symbol: the strategy security selected in StockSharp.
- Timeframes: both modules use 6-hour candles by default (configurable through parameters).
- Order size: each module sends market orders with a separate volume parameter (default `1`).

## Indicators and signal logic

### SilverTrend block

1. Builds a rolling price channel from the last `SSP` candles.
2. Applies the original `Risk` shift `(33 - Risk) / 100` to move channel borders inside the high/low range.
3. Colors every candle according to the active trend (`0`/`1` bullish, `3`/`4` bearish, `2` neutral) just like the MetaTrader indicator.
4. Signals:
   - **Long** when the candle at the configured `Signal Bar` becomes bullish while the previous bar was not (`color < 2` and previous `> 1`).
   - **Short** when it turns bearish while the previous bar was not (`color > 2` and previous `< 3`).
5. Optional stop-loss and take-profit levels are measured in points using the security price step.

### ColorJFatl block

1. Builds a FATL value by applying the official coefficient table to the chosen `Applied Price` source.
2. Smooths the result with an EMA of length `JMA Length` (the Jurik phase input is preserved for compatibility and documentation purposes).
3. Colors the FATL line according to slope: `2` for rising, `0` for falling, and `1` for flat segments.
4. Signals:
   - **Long** when the FATL color turns to `2` while the previous color was `0` or `1`.
   - **Short** when the color turns to `0` while the previous value was `1` or `2`.
5. Each direction may optionally close the opposite block position before opening a new trade.

## Risk management

- SilverTrend and ColorJFatl each maintain their own entry price and stop/target distances.
- If a stop or target is hit, only the affected block closes its virtual position (the other block may stay open).
- When both blocks agree on the same direction, their volumes accumulate.

## Parameters

| Group | Name | Description |
| --- | --- | --- |
| SilverTrend | `Silver Candle Type` | Candle subscription used for the SilverTrend indicator. |
| SilverTrend | `SSP` | Length of the rolling high/low range. |
| SilverTrend | `Risk` | Channel contraction factor (original `Risk` input). |
| SilverTrend | `Signal Bar` | Bar shift used for the signal (0 = current closed bar, 1 = previous bar, etc.). |
| SilverTrend | `Allow Silver Long/Short` | Enable entries for each direction. |
| SilverTrend | `Close Silver Long/Short` | Allow automatic closing of the opposite position. |
| SilverTrend | `Silver Volume` | Volume for trades opened by the SilverTrend block. |
| SilverTrend | `Silver SL/TP` | Stop-loss and take-profit distances in points. |
| ColorJFatl | `Color Candle Type` | Candle subscription used for the FATL calculations. |
| ColorJFatl | `JMA Length` | Length of the EMA smoother that emulates JMA. |
| ColorJFatl | `JMA Phase` | Preserved for completeness (no direct influence inside StockSharp). |
| ColorJFatl | `Applied Price` | Source price (close, median, typical, trend-follow, etc.). |
| ColorJFatl | `Digits` | Decimal precision applied to the FATL value. |
| ColorJFatl | `Color Signal Bar` | Bar shift used for FATL signals. |
| ColorJFatl | `Allow/Close` toggles | Enable entries and auto exits for each direction. |
| ColorJFatl | `Color Volume` | Volume for trades opened by the ColorJFatl block. |
| ColorJFatl | `Color SL/TP` | Stop-loss and take-profit distances in points for the block. |

## Notes

- The strategy subscribes to both candle streams even if they are identical. Duplicate subscriptions are handled internally by StockSharp.
- The Jurik phase parameter is retained to stay close to the original expert advisor. StockSharp's EMA-based smoother replicates the curved FATL behaviour while keeping the parameter available for future extensions.
- Make sure the security has `PriceStep` set in order to use point-based risk limits.

## Usage tips

1. Set the `Volume` property of the strategy or adjust block-specific volume parameters to control absolute exposure.
2. Use the enable/disable flags to test each block separately before combining them.
3. Because the blocks operate independently, the strategy can hold a net long and short simultaneously (for example, long from SilverTrend and short from ColorJFatl) – the resulting position is the algebraic sum of both.
4. Optimize `SSP`, `Risk`, and `JMA Length` for the target market if you plan to use automated parameter search.
