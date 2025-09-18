# FitFul 13 Time Gated Strategy

## Overview
The **FitFul 13 Time Gated Strategy** is a StockSharp port of the MetaTrader 4 expert advisor "FitFul_13". The strategy builds a weekly pivot ladder (PP, R0.5, R1, R1.5, R2, R2.5, R3 and the corresponding support levels) using the previous week's high, low and close. Trade decisions are taken on the primary timeframe (default 1 hour) and are optionally confirmed by a faster timeframe (default 15 minutes). New positions are allowed only at specific intraday minutes to mimic the original EA behaviour.

## Signal logic
1. **Weekly pivot calculation**
   - At the close of every weekly candle the pivot ladder is recalculated.
   - Stop-loss and take-profit prices are offset from the base levels by a configurable distance expressed in price points.
2. **Primary timeframe conditions**
   - The last completed primary candle must be bullish to search for long entries or bearish to search for short entries.
   - The previous primary candle must straddle one of the pivot levels (open below and close above for longs, open above and close below for shorts).
3. **Confirmation timeframe conditions**
   - If the current confirmation candle is bullish, the lows of the two previous confirmation candles must pierce and close above the same pivot level to confirm a long signal.
   - If the current confirmation candle is bearish, the highs of the two previous confirmation candles must pierce and close below a pivot level to confirm a short signal.
4. **Entry timing**
   - A trade is placed only when the opening minute of the finished primary candle equals one of the four configured minutes (0, 15, 30 or 45 by default).
   - Net exposure is capped by `MaxNetPositions × Volume` to emulate the "maximum three open orders" constraint of the MetaTrader version.

## Risk management
- **Stops and targets** – Every position is assigned a pivot-derived stop-loss and take-profit immediately after entry.
- **Trailing stop** – Once price advances by the configured number of points, the stop is trailed in the trade direction.
- **Maximum holding time** – Profitable trades are closed once the holding time exceeds the configured duration (48 hours by default).
- **Friday flat rule** – On Fridays, any open position is closed between the configured minutes of the specified hour (default 21:50–21:59).

## Parameters
| Name | Description |
| --- | --- |
| `PrimaryCandleType` | Timeframe used for the main pivot cross checks. |
| `ConfirmationCandleType` | Faster timeframe that validates pivot reactions. |
| `Volume` | Net market order volume. |
| `MaxNetPositions` | Maximum exposure measured in multiples of `Volume`. |
| `OffsetPoints` | Price-point distance applied to stops and targets around each pivot. |
| `TrailingStopPoints` | Trailing stop distance in price points. |
| `CloseAfter` | Maximum holding time for profitable positions. |
| `CloseHour`, `CloseMinuteFrom`, `CloseMinuteTo` | Friday time window for forced exits. |
| `EntryMinute0..3` | Allowed minutes (within every hour) for opening new positions. |

## Notes
- The conversion keeps the original EA's reliance on the previous week's pivot ladder and quarter-hour execution windows.
- Money management has been simplified: the StockSharp `Volume` parameter controls order size directly instead of re-implementing the dynamic lot calculation from MetaTrader.
- All comments inside the code are written in English, as required by the project guidelines.
