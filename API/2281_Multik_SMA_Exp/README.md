# Multik SMA Exp Strategy

## Overview
This strategy implements a contrarian approach based on the slope of a simple moving average (SMA). It was ported from the MetaTrader 5 expert advisor "Multik_SMA_Exp".

The strategy monitors the last three SMA values. If the SMA has been falling for the two most recent completed segments, the strategy enters a long position. If the SMA has been rising for the two segments, it opens a short position. Positions are closed when the slope of the SMA reverses.

## Parameters
- **MA Period** – length of the simple moving average. Default: 50.
- **Candle Type** – type of candles used for calculations. Default: 1-minute timeframe.

## Trading Rules
1. On each finished candle, compute the SMA.
2. Determine slopes:
   - `dsma1 = SMA[n-1] - SMA[n-2]`
   - `dsma2 = SMA[n-2] - SMA[n-3]`
3. Entry:
   - If `dsma1 < 0` and `dsma2 < 0` and there is no long position, buy.
   - If `dsma1 > 0` and `dsma2 > 0` and there is no short position, sell.
4. Exit:
   - If holding a long and `dsma1 > 0`, close the long.
   - If holding a short and `dsma1 < 0`, close the short.

The volume of new orders uses the strategy’s `Volume` plus the absolute value of the current position to fully reverse when necessary.
