# Fast Slow RVI Crossover Strategy

## Overview
This strategy replicates the MetaTrader expert advisor `_HPCS_FastSlowRVIsCrossOver_MT4_EA_V01_WE`. It trades when the Relative Vigor Index (RVI) main line crosses its signal line during the configured trading session. Only one trade is allowed per candle, and the strategy supports optional stop loss, take profit, and trailing stop distances expressed in pips.

## Trading Logic
1. Build standard time-based candles selected by the **Candle Type** parameter.
2. Calculate the RVI with the configured **RVI Period** and a 4-period simple moving average as the signal line.
3. When the RVI rises above the signal line, close any short position and open/scale into a long position.
4. When the RVI falls below the signal line, close any long position and open/scale into a short position.
5. Ignore signals that appear outside of the **Start Time** and **Stop Time** interval.
6. Place protective orders according to the selected risk parameters. Trailing stops are managed by the StockSharp protection engine.
7. Avoid duplicate entries on the same candle by reacting only once per bar.

## Parameters
| Name | Description |
|------|-------------|
| **RVI Period** | Number of bars used by the Relative Vigor Index. |
| **Take Profit (pips)** | Optional take-profit distance measured in pips. Set to zero to disable. |
| **Stop Loss (pips)** | Optional stop-loss distance measured in pips. Set to zero to disable. |
| **Trailing Stop (pips)** | Optional trailing stop distance in pips. Set to zero to disable trailing. |
| **Trailing Step (pips)** | Minimum favorable move required before the trailing stop is tightened. Works only when the trailing stop is active. |
| **Volume** | Order volume submitted on each entry. |
| **Candle Type** | Time frame or custom candle data type used for analysis. |
| **Start Time** | Beginning of the daily trading window (inclusive). |
| **Stop Time** | End of the daily trading window (exclusive). |

## Notes
- The pip size is adapted to the security tick size to match MetaTrader point handling (5- and 3-digit symbols use a 10× multiplier).
- Call `StartProtection` once inside `OnStarted` to enable protective orders and trailing management.
- All comments in the source code are written in English, as required by the project guidelines.
