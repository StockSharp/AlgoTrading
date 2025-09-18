# RSI Dual Cloud Strategy

## Overview
The **RSI Dual Cloud Strategy** is a StockSharp port of the MetaTrader expert advisor “RSI Dual Cloud EA”.
It trades on a configurable candle series and analyses two Relative Strength Index (RSI) calculations – a fast
and a slow line. Signals are generated when the fast RSI enters, stays within, or leaves a defined oversold/overbought
zone, or when the fast line crosses the slow line. The strategy can optionally invert its signals and can be restricted
to long-only or short-only operation.

The strategy operates with market orders only. When a new signal is received the existing position in the opposite
direction is closed before opening a fresh position. Position sizing is controlled through a single volume parameter.

## Signal Logic
1. **Entrance signal** – triggers when the fast RSI crosses into the zone:
   - Long: previous RSI above the lower level and current RSI below it.
   - Short: previous RSI below the upper level and current RSI above it.
2. **Being signal** – triggers as long as the fast RSI remains inside the zone:
   - Long: fast RSI below the lower level.
   - Short: fast RSI above the upper level.
3. **Leaving signal** – triggers when the fast RSI exits the zone:
   - Long: previous RSI below the lower level and current RSI above it.
   - Short: previous RSI above the upper level and current RSI below it.
4. **Crossing signal** – uses the dual cloud behaviour:
   - Long: fast RSI crossing above the slow RSI.
   - Short: fast RSI crossing below the slow RSI.

Any combination of the four conditions can be enabled. At least one condition should be active for entries to occur.
When the **Reverse** option is enabled, long and short signals are swapped.

## Parameters
| Name | Description |
| --- | --- |
| **Candle Type** | The candle series used for calculations (default: 1 hour). |
| **Fast RSI / Slow RSI** | Periods for the fast and slow RSI calculations. |
| **Upper Level / Lower Level** | RSI thresholds for the overbought and oversold zones. |
| **Order Volume** | Volume for market orders. |
| **Use Entrance / Being / Leaving / Crossing** | Toggles for each signal family. |
| **Closed Candles** | If enabled, signals are only evaluated on finished candles. |
| **Reverse** | Swaps long and short signals. |
| **Trade Mode** | Limits trading to long, short, or both directions. |

## Usage Notes
- The strategy subscribes to a single candle series and runs two RSI indicators bound through the high-level API.
- Only market orders are used; any open exposure in the opposite direction is closed before a new trade is placed.
- The default configuration matches the original expert advisor (fast RSI 5, slow RSI 15, levels 25/75).
- Combine the signal toggles to reproduce the indicator combinations from the MetaTrader version.
