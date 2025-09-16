# Caudate X Period Candle TM Plus Strategy

## Overview
The strategy replicates the logic of the Caudate X Period Candle TM Plus expert advisor. It smooths the candle open, high, low and close prices with a configurable moving average, builds a Donchian-style range and classifies each finished candle into one of six color codes depending on the position of the body inside the range. Long entries are triggered by the bullish lower-tail colors (0 or 1), while short entries are triggered by the bearish upper-tail colors (5 or 6). Opposite color groups are used to exit existing positions.

## Trading rules
1. Subscribe to the selected candle series and smooth each component with the chosen moving average.
2. Compute the highest high and lowest low of the smoothed highs and lows over the specified `Donchian Period`, then expand the range so that it always contains the smoothed open and close.
3. Determine the candle color:
   * Colors **0/1** – body near the top of the range (lower tail).
   * Colors **2/4** – body centered inside the range.
   * Colors **5/6** – body near the bottom of the range (upper tail).
4. Evaluate the color of the bar offset by `Signal Bar` (default `1` uses the previous completed candle).
5. Open positions when the color belongs to the entry group and the opposite position is not active.
6. Close positions when the color belongs to the exit group or the maximum holding time expires.
7. Optional stop-loss and take-profit offsets are set through the built-in protection module.

## Parameters
| Parameter | Description |
| --- | --- |
| `Candle Type` | Time frame used for signal calculations. |
| `Donchian Period` | Number of candles for the smoothed high/low range. |
| `Signal Bar` | Number of bars to delay signal evaluation (0 = current bar). |
| `Smoothing Method` | Moving average applied to OHLC prices (SMA, EMA, SMMA, LWMA, Jurik JJMA approximation, Kaufman AMA). |
| `MA Length` | Length of the smoothing filter. |
| `MA Phase` | Reserved for JJMA compatibility (not used by StockSharp averages). |
| `Enable Long/Short Entries` | Toggle opening new long or short positions. |
| `Enable Long/Short Exits` | Toggle closing existing long or short positions on signals. |
| `Enable Time Exit` | Enable the maximum holding time filter. |
| `Time Exit (minutes)` | Holding duration before a forced exit. |
| `Stop Loss (points)` | Stop-loss distance in price steps (multiplied by `Security.PriceStep`). |
| `Take Profit (points)` | Take-profit distance in price steps. |

## Notes
- `Signal Bar = 1` matches the MQL5 expert behaviour by acting on the last fully closed candle.
- When stop or target distances are greater than zero the strategy calls `StartProtection` with absolute offsets based on the instrument price step.
- `MA Phase` is kept for compatibility but is not consumed by the StockSharp moving-average implementations.
- Set the base order size through the inherited `Strategy.Volume` property; the implementation always closes opposite positions before opening a new one.
